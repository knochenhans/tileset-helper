#Requires -Version 5
<#
.SYNOPSIS
  Build the tileset-helper editor extension and optionally deploy it into a project.

.DESCRIPTION
  After you pull a new version of the source, run this to rebuild the native library.
  SCons writes the build output into the repo's bin folder. Pass -Dest to also copy the
  runtime files (the .dll and the .gdextension) into your project's bin folder.

  This is an editor-only plugin, so the default target is template_debug - the Godot
  editor loads that build. When deploying, the script first clears any "~" reload shadow
  in the destination so the editor picks up the new build on the next project reload.

.EXAMPLE
  .\update.ps1
  Build the debug template (no deploy).

.EXAMPLE
  .\update.ps1 -Dest "C:\MyGame\bin"
  Build and copy the runtime files into C:\MyGame\bin.
#>
param(
	[ValidateSet("template_debug", "template_release", "editor", "all")]
	[string]$Target = "template_debug",
	[string]$Dest = ""
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$SourceBin = Join-Path $RepoRoot "bin"

$targets = if ($Target -eq "all") { @("template_debug", "template_release") } else { @($Target) }

Push-Location $RepoRoot
try {
	foreach ($t in $targets) {
		Write-Host "==> Building platform=windows target=$t" -ForegroundColor Cyan
		& scons platform=windows target=$t
		if ($LASTEXITCODE -ne 0) { throw "scons failed for target=$t (exit $LASTEXITCODE)" }
	}
}
finally {
	Pop-Location
}

if ($Dest -ne "") {
	if (-not (Test-Path $Dest)) {
		New-Item -ItemType Directory -Force -Path $Dest | Out-Null
	}
	Write-Host "==> Deploying runtime files to $Dest" -ForegroundColor Cyan

	# Clear the editor's reload shadow copies so the new build is picked up.
	Get-ChildItem -Path $Dest -File -Filter "~libtileset_helper.*" -ErrorAction SilentlyContinue |
		ForEach-Object { Remove-Item $_.FullName -Force }

	# Native libraries (build output).
	Get-ChildItem -Path $SourceBin -File |
		Where-Object { $_.Name -like "libtileset_helper.*.dll" } |
		ForEach-Object { Copy-Item $_.FullName -Destination $Dest -Force }

	# The extension descriptor (tracked in bin/, alongside the build output).
	Get-ChildItem -Path $SourceBin -File -Filter "tileset_helper.gdextension*" -ErrorAction SilentlyContinue |
		ForEach-Object { Copy-Item $_.FullName -Destination $Dest -Force }

	Write-Host "Done. Reload the project in the Godot editor to pick up the new build." -ForegroundColor Green
}
