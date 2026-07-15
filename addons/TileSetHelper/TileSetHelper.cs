#if TOOLS
using Godot;
using Godot.Collections;

[Tool]
public partial class TileSetHelper : EditorPlugin
{
	private EditorDock dockInstance;
	private Sync syncUI;

	public override void _EnterTree()
	{
		dockInstance = new EditorDock
		{
			Name = "TileSet Helper",
			Title = "TileSet Helper",
			AvailableLayouts = EditorDock.DockLayout.Horizontal | EditorDock.DockLayout.Vertical,
			DefaultSlot = EditorDock.DockSlot.Bottom,
			CustomMinimumSize = new Vector2(0, 150)
		};

		var packed = GD.Load<PackedScene>("res://addons/TileSetHelper/Sync.tscn");
		if (packed != null)
		{
			syncUI = (Sync)packed.Instantiate();

			syncUI.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			syncUI.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

			dockInstance.AddChild(syncUI);
		}
		else
		{
			GD.PrintErr("TileSetHelper: Failed to load Sync.tscn");
		}

		AddDock(dockInstance);

		EditorInterface.Singleton.GetSelection().SelectionChanged += OnSelectionChanged;
	}

	private void OnSelectionChanged()
	{
		if (syncUI == null) return;

		Array selectedNodes = (Array)EditorInterface.Singleton.GetSelection().GetSelectedNodes();
		foreach (var obj in selectedNodes)
		{
			Node node = (Node)(GodotObject)obj;
			if (node is TileMapLayer tileMapLayer)
			{
				if (tileMapLayer.TileSet == null) continue;

				syncUI.tileMapLayer = tileMapLayer;
				syncUI.tileSet = tileMapLayer.TileSet;

				syncUI.UpdateView();
				return;
			}
		}

		// _syncUI.ClearView(); 
	}

	public override void _ExitTree()
	{
		EditorInterface.Singleton.GetSelection().SelectionChanged -= OnSelectionChanged;

		if (dockInstance != null)
		{
			RemoveDock(dockInstance);
			dockInstance.Free();
			dockInstance = null;
		}
	}
}
#endif