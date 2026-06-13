#Requires -Version 5
<#
.SYNOPSIS
  Assemble the redistributable addon under dist/addons/tileset_helper.

.DESCRIPTION
  Builds the Windows debug and release libraries, then stages everything an end
  user needs (the two .dll files, a distribution .gdextension that points at the
  addons path, the README and the LICENSE) under dist/addons/tileset_helper.
  Zip the dist/addons folder for the Godot Asset Library.

.PARAMETER SkipBuild
  Assemble from the libraries already in bin/ without rebuilding.

.PARAMETER Zip
  Also produce dist/tileset_helper-windows.zip (contains the addons/ folder).

.EXAMPLE
  .\package.ps1
  Build both targets and assemble the addon.

.EXAMPLE
  .\package.ps1 -SkipBuild -Zip
  Assemble from existing builds and produce the zip.
#>
param(
	[switch]$SkipBuild,
	[switch]$Zip
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$Bin = Join-Path $RepoRoot "bin"
$Pkg = Join-Path $RepoRoot "dist/addons/tileset_helper"
$PkgBin = Join-Path $Pkg "bin"

$debug = Join-Path $Bin "libtileset_helper.windows.template_debug.x86_64.dll"
$release = Join-Path $Bin "libtileset_helper.windows.template_release.x86_64.dll"

if (-not $SkipBuild) {
	& (Join-Path $PSScriptRoot "update.ps1") -Target all
}

foreach ($f in @($debug, $release)) {
	if (-not (Test-Path $f)) { throw "Missing $([System.IO.Path]::GetFileName($f)) - build first (omit -SkipBuild)." }
}

# Clean and recreate the package tree.
if (Test-Path $Pkg) { Remove-Item $Pkg -Recurse -Force }
New-Item -ItemType Directory -Force -Path $PkgBin | Out-Null

Write-Host "==> Staging addon to $Pkg" -ForegroundColor Cyan
Copy-Item $debug -Destination $PkgBin -Force
Copy-Item $release -Destination $PkgBin -Force

# Distribution descriptor: addons path, Windows-only, requires Godot 4.6+.
$gdext = @"
[configuration]

entry_symbol = "tileset_helper_library_init"
compatibility_minimum = "4.6"
reloadable = true

[libraries]

windows.debug.x86_64 = "res://addons/tileset_helper/bin/libtileset_helper.windows.template_debug.x86_64.dll"
windows.release.x86_64 = "res://addons/tileset_helper/bin/libtileset_helper.windows.template_release.x86_64.dll"
"@
# Write UTF-8 without BOM so Godot parses it cleanly.
[System.IO.File]::WriteAllText((Join-Path $Pkg "tileset_helper.gdextension"), $gdext, (New-Object System.Text.UTF8Encoding($false)))

# README and LICENSE (whatever extension the license uses).
$readme = Join-Path $RepoRoot "README.md"
if (Test-Path $readme) { Copy-Item $readme -Destination $Pkg -Force }
Get-ChildItem -Path $RepoRoot -File -Filter "LICENSE*" -ErrorAction SilentlyContinue |
	ForEach-Object { Copy-Item $_.FullName -Destination $Pkg -Force }

if ($Zip) {
	$zipPath = Join-Path $RepoRoot "dist/tileset_helper-windows.zip"
	if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
	Compress-Archive -Path (Join-Path $RepoRoot "dist/addons") -DestinationPath $zipPath
	Write-Host "==> Wrote $zipPath" -ForegroundColor Cyan
}

Write-Host "Done. Package contents:" -ForegroundColor Green
Get-ChildItem -Path $Pkg -Recurse -File | ForEach-Object { "  " + $_.FullName.Substring($RepoRoot.Length + 1) }
