#if TOOLS
using Godot;
using Godot.Collections;

[Tool]
public partial class TileSetHelper : EditorPlugin
{
	private EditorDock dockInstance;
	private Button syncButton;

	public override void _EnterTree()
	{
		dockInstance = new EditorDock
		{
			Name = "TileSet Helper",
			Title = "TileSet Helper",
			AvailableLayouts = EditorDock.DockLayout.Horizontal | EditorDock.DockLayout.Vertical,
			DefaultSlot = EditorDock.DockSlot.Bottom, // Places it cleanly in the bottom panel
			CustomMinimumSize = new Vector2(0, 150)
		};

		syncButton = new Button
		{
			Text = "Sync TileSet",
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
		};
		syncButton.Pressed += OnEditorButtonPressed;

		dockInstance.AddChild(syncButton);

		AddDock(dockInstance);
	}

	private void OnEditorButtonPressed()
	{
		Array selectedNodes = (Array)EditorInterface.Singleton.GetSelection().GetSelectedNodes();
		foreach (var obj in selectedNodes)
		{
			Node node = (Node)(GodotObject)obj;
			if (node == null) continue;

			if (node is TileMapLayer tileMapLayer)
			{
				if (tileMapLayer.TileSet == null) continue;

				var packed = GD.Load<PackedScene>("res://addons/TileSetHelper/Sync.tscn");
				if (packed == null) continue;

				Sync sync = (Sync)packed.Instantiate();
				sync.tileMapLayer = tileMapLayer;
				sync.tileSet = tileMapLayer.TileSet;

				AddChild(sync);
				sync.UpdateView();
				break;
			}
		}
	}

	public override void _ExitTree()
	{
		if (dockInstance != null)
		{
			RemoveDock(dockInstance);
			dockInstance.Free();
			dockInstance = null;
		}
	}
}
#endif