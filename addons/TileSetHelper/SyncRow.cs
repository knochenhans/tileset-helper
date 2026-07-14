using Godot;
using System;

[Tool]
public partial class SyncRow : HBoxContainer
{
    [Export] private Label title;
    [Export] private Button copy;
    [Export] private Button paste;
}
