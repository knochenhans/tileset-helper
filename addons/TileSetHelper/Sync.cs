using Godot;
using System;
using Godot.Collections;
using System.Linq;

[Tool]
public partial class Sync : AcceptDialog
{
	[Export] private PackedScene syncRow;

	[Export] private VBoxContainer SourceNode;
	[Export] private VBoxContainer PhysicNode;
	[Export] private VBoxContainer TerrainNode;
	[Export] private VBoxContainer NavigationNode;
	[Export] private VBoxContainer CustomDataNode;
	[Export] private VBoxContainer OccluderNode;

	private Node root;


	public TileMapLayer tileMapLayer;
	public TileSet tileSet;
	private Dictionary tilesData = [];
	private Dictionary copyTilesData = [];
	private Dictionary pasteTilesData = [];
	private int currentSourceIndex = -1;
	private int currentSourceId = -1;
	private int copyLayerID = -1;
	private int copyTerrainMode = -1;
	private string copyCustomDataLayerName = "";

	public override void _Ready()
	{
		root = GetTree().Root;
	}

	public override void _ExitTree()
	{
		var syncData = new Dictionary
		{
			["copy_tiles_data"] = copyTilesData,
			["copy_layer_id"] = copyLayerID,
			["copy_terrain_mode"] = copyTerrainMode,
			["copy_custom_data_layer_name"] = copyCustomDataLayerName,
		};
		root.SetMeta("sync_data", syncData);
	}

	private void ReadData()
	{
		if (!root.HasMeta("sync_data"))
		{
			root.SetMeta("sync_data", new Dictionary());
			return;
		}
		var syncData = (Dictionary)root.GetMeta("sync_data");
		if (syncData.ContainsKey("copy_tiles_data")) copyTilesData = (Dictionary)syncData["copy_tiles_data"];
		if (syncData.ContainsKey("copy_layer_id")) copyLayerID = (int)syncData["copy_layer_id"];
		if (syncData.ContainsKey("copy_terrain_mode")) copyTerrainMode = (int)syncData["copy_terrain_mode"];
		if (syncData.ContainsKey("copy_custom_data_layer_name")) copyCustomDataLayerName = (string)syncData["copy_custom_data_layer_name"];
	}

	public void UpdateView()
	{
		ReadData();
		CountTilesData();
		var buttonGroup = new ButtonGroup();
		var sourceCount = tileSet.GetSourceCount();
		for (int sourceIndex = 0; sourceIndex < sourceCount; sourceIndex++)
		{
			int sourceId = tileSet.GetSourceId(sourceIndex);
			var check_box = new CheckBox
			{
				ButtonGroup = buttonGroup,
				Text = "Source " + sourceId
			};
			SourceNode.AddChild(check_box);
			SourceNode.AddChild(new HSeparator());
			check_box.Pressed += () => OnUpdateSourceIndex(sourceIndex);
			if (sourceIndex == 0)
			{
				check_box.ButtonPressed = true;
				OnUpdateSourceIndex(sourceIndex);
			}
		}

		var physicsLayers = tileSet.GetPhysicsLayersCount();
		for (int layerIDPhysics = 0; layerIDPhysics < physicsLayers; layerIDPhysics++)
		{
			var syncRowPhysicsInstance = (HBoxContainer)syncRow.Instantiate();
			PhysicNode.AddChild(syncRowPhysicsInstance);
			syncRowPhysicsInstance.GetNode<Label>("Title").Text = "Layer " + layerIDPhysics;
			syncRowPhysicsInstance.GetNode<Button>("Copy").Pressed += () => OnLayerCopy(layerIDPhysics);
			syncRowPhysicsInstance.GetNode<Button>("Paste").Pressed += () => OnPhysicsPaste(layerIDPhysics);
			PhysicNode.Visible = physicsLayers > 0;

			var terrainSets = tileSet.GetTerrainSetsCount();
			for (int terrainSet = 0; terrainSet < terrainSets; terrainSet++)
			{
				var terrainMode = tileSet.GetTerrainSetMode(terrainSet);
				var terrainTitle = new Label
				{
					Text = "Terrain Set " + terrainSet + " - " + "Mode " + (int)terrainMode
				};
				TerrainNode.AddChild(terrainTitle);
				var terrains = tileSet.GetTerrainsCount(terrainSet);
				for (int terrainIndex = 0; terrainIndex < terrains; terrainIndex++)
				{
					var syncRowInstance = (HBoxContainer)syncRow.Instantiate();
					TerrainNode.AddChild(syncRowInstance);
					syncRowInstance.GetNode<Label>("Title").Text = tileSet.GetTerrainName(terrainSet, terrainIndex);
					syncRowInstance.GetNode<Button>("Copy").Pressed += () => OnTerrainCopy((int)terrainMode);
					syncRowInstance.GetNode<Button>("Paste").Pressed += () => OnTerrainPaste((int)terrainMode, terrainSet, terrainIndex);
				}
				TerrainNode.Visible = terrainSets > 0;

				var navigationLayers = tileSet.GetNavigationLayersCount();
				for (int layerID = 0; layerID < navigationLayers; layerID++)
				{
					var syncRowInstance = (HBoxContainer)syncRow.Instantiate();
					NavigationNode.AddChild(syncRowInstance);
					syncRowInstance.GetNode<Label>("Title").Text = "Layer " + layerID;
					syncRowInstance.GetNode<Button>("Copy").Pressed += () => OnLayerCopy(layerID);
					syncRowInstance.GetNode<Button>("Paste").Pressed += () => OnNavigationPaste(layerID);
				}
				NavigationNode.Visible = navigationLayers > 0;

				var custom_data_layers = tileSet.GetCustomDataLayersCount();
				for (int layerID = 0; layerID < custom_data_layers; layerID++)
				{
					var syncRowInstance = (HBoxContainer)syncRow.Instantiate();
					CustomDataNode.AddChild(syncRowInstance);
					var customDataName = tileSet.GetCustomDataLayerName(layerID);
					syncRowInstance.GetNode<Label>("Title").Text = "Custom Data - " + customDataName;
					syncRowInstance.GetNode<Button>("Copy").Pressed += () => OnLayerCopy(layerID);
					syncRowInstance.GetNode<Button>("Copy").Pressed += () => OnCustomDataCopy(customDataName);
					syncRowInstance.GetNode<Button>("Paste").Pressed += () => OnCustomDataPaste(layerID);
				}
				CustomDataNode.Visible = custom_data_layers > 0;

				var occluder_layers = tileSet.GetOcclusionLayersCount();
				for (int layerID = 0; layerID < occluder_layers; layerID++)
				{
					var syncRowInstance = (HBoxContainer)syncRow.Instantiate();
					OccluderNode.AddChild(syncRowInstance);
					syncRowInstance.GetNode<Label>("Title").Text = "Custom Data - " + layerID;
					syncRowInstance.GetNode<Button>("Copy").Pressed += () => OnLayerCopy(layerID);
					syncRowInstance.GetNode<Button>("Paste").Pressed += () => OnOccluderPaste(layerID);
				}
				OccluderNode.Visible = occluder_layers > 0;

				var titles = FindChildren("Title");
				foreach (Button node in titles.Cast<Button>())
				{
					node.Pressed += () => OnButtonPressed(node);
					if (node.GetParent() is Control parent && parent.Visible)
					{
						parent.AddSibling(new HSeparator());
					}
				}
			}
		}
	}

	private void CountTilesData()
	{
		tilesData.Clear();
		var sourceCount = tileSet.GetSourceCount();
		for (int sourceIndex = 0; sourceIndex < sourceCount; sourceIndex++)
		{
			var sourceId = tileSet.GetSourceId(sourceIndex);
			var atlas_source = (TileSetAtlasSource)tileSet.GetSource(sourceId);
			var tilesCount = atlas_source.GetTilesCount();
			for (int tileIndex = 0; tileIndex < tilesCount; tileIndex++)
			{
				var atlasCoords = atlas_source.GetTileId(tileIndex);
				var alternativeTilesCount = atlas_source.GetAlternativeTilesCount(atlasCoords);
				for (int alternativeTileIndex = 0; alternativeTileIndex < alternativeTilesCount; alternativeTileIndex++)
				{
					var alternativeTile = atlas_source.GetAlternativeTileId(atlasCoords, alternativeTileIndex);
					var tileData = atlas_source.GetTileData(atlasCoords, alternativeTile);
					tilesData[new Dictionary { { "source_id", sourceId }, { "atlas_coords", atlasCoords }, { "alternative_tile", alternativeTile } }] = tileData;
				}
			}
		}
	}

	private Dictionary GetCurrentSourceTilesData()
	{
		var sourceTilesData = new Dictionary();
		foreach (Dictionary key in tilesData.Keys)
		{
			int sourceID = (int)key["source_id"];
			if (currentSourceId == sourceID)
			{
				var sourceKey = new Dictionary();
				foreach (var k in key.Keys)
				{
					sourceKey[k] = key[k];
				}
				sourceKey.Remove("source_id");
				sourceTilesData[sourceKey] = tilesData[key];
			}
		}
		return sourceTilesData;
	}

	private void OnUpdateSourceIndex(int source_index)
	{
		if (currentSourceIndex != source_index)
		{
			currentSourceIndex = source_index;
			currentSourceId = tileSet.GetSourceId(source_index);
			pasteTilesData = GetCurrentSourceTilesData();
		}
	}

	private void OnLayerCopy(int layer_id)
	{
		copyTilesData = GetCurrentSourceTilesData();
		copyLayerID = layer_id;
	}

	private void OnTerrainCopy(int terrain_mode)
	{
		copyTilesData = GetCurrentSourceTilesData();
		copyTerrainMode = terrain_mode;
	}

	private void OnCustomDataCopy(string layer_name)
	{
		copyCustomDataLayerName = layer_name;
	}

	private void OnPhysicsPaste(int layer_id)
	{
		foreach (Dictionary key in pasteTilesData.Keys.Select(v => (Dictionary)v))
		{
			if (!copyTilesData.ContainsKey(key)) continue;
			TileData pasteTileData = (TileData)(GodotObject)pasteTilesData[key];
			TileData copyTileData = (TileData)(GodotObject)copyTilesData[key];
			pasteTileData.SetConstantLinearVelocity(layer_id, copyTileData.GetConstantLinearVelocity(copyLayerID));
			pasteTileData.SetConstantAngularVelocity(layer_id, copyTileData.GetConstantAngularVelocity(copyLayerID));
			int pastePolygonsCount = pasteTileData.GetCollisionPolygonsCount(layer_id);
			int copyPolygonsCount = copyTileData.GetCollisionPolygonsCount(copyLayerID);
			for (int polygonIndex = 0; polygonIndex < pastePolygonsCount; polygonIndex++)
			{
				pasteTileData.RemoveCollisionPolygon(layer_id, 0);
			}
			for (int polygonIndex = 0; polygonIndex < copyPolygonsCount; polygonIndex++)
			{
				pasteTileData.AddCollisionPolygon(layer_id);
				pasteTileData.SetCollisionPolygonPoints(layer_id, polygonIndex, copyTileData.GetCollisionPolygonPoints(copyLayerID, polygonIndex));
				pasteTileData.SetCollisionPolygonOneWay(layer_id, polygonIndex, copyTileData.IsCollisionPolygonOneWay(copyLayerID, polygonIndex));
				pasteTileData.SetCollisionPolygonOneWayMargin(layer_id, polygonIndex, copyTileData.GetCollisionPolygonOneWayMargin(copyLayerID, polygonIndex));
			}
		}
	}

	private void OnTerrainPaste(int terrain_mode, int terrain_set, int terrain)
	{
		if (terrain_mode != copyTerrainMode) return;
		foreach (Dictionary key in pasteTilesData.Keys.Select(v => (Dictionary)v))
		{
			if (!copyTilesData.ContainsKey(key)) continue;
			TileData pasteTileData = (TileData)(GodotObject)pasteTilesData[key];
			TileData copyTileData = (TileData)(GodotObject)copyTilesData[key];
			if (copyTileData.TerrainSet == -1) continue;
			pasteTileData.TerrainSet = terrain_set;
			if (copyTileData.Terrain != -1) pasteTileData.Terrain = terrain;
			for (int peering_bit = 0; peering_bit < 16; peering_bit++)
			{
				if (copyTileData.IsValidTerrainPeeringBit((TileSet.CellNeighbor)peering_bit))
				{
					int current_terrain = copyTileData.GetTerrainPeeringBit((TileSet.CellNeighbor)peering_bit);
					if (current_terrain != -1)
					{
						pasteTileData.SetTerrainPeeringBit((TileSet.CellNeighbor)peering_bit, terrain);
					}
				}
			}
		}
	}

	private void OnNavigationPaste(int layer_id)
	{
		foreach (Dictionary key in pasteTilesData.Keys.Select(v => (Dictionary)v))
		{
			if (!copyTilesData.ContainsKey(key)) continue;
			TileData pasteTileData = (TileData)(GodotObject)pasteTilesData[key];
			TileData copyTileData = (TileData)(GodotObject)copyTilesData[key];
			NavigationPolygon pasteNavigationPolygon = pasteTileData.GetNavigationPolygon(layer_id);
			NavigationPolygon copyNavigationPolygon = copyTileData.GetNavigationPolygon(copyLayerID);
			pasteTileData.SetNavigationPolygon(layer_id, copyNavigationPolygon);
		}
	}

	private void OnCustomDataPaste(int layer_id)
	{
		if (!tileSet.HasCustomDataLayerByName(copyCustomDataLayerName)) return;
		foreach (Dictionary key in pasteTilesData.Keys.Select(v => (Dictionary)v))
		{
			if (!copyTilesData.ContainsKey(key)) continue;
			TileData pasteTileData = (TileData)(GodotObject)pasteTilesData[key];
			TileData copyTileData = (TileData)(GodotObject)copyTilesData[key];
			var customData = copyTileData.GetCustomData(copyCustomDataLayerName);
			pasteTileData.SetCustomData(copyCustomDataLayerName, customData);
		}
	}

	private void OnOccluderPaste(int layer_id)
	{
		foreach (Dictionary key in pasteTilesData.Keys.Select(v => (Dictionary)v))
		{
			if (!copyTilesData.ContainsKey(key)) continue;
			TileData pasteTileData = (TileData)(GodotObject)pasteTilesData[key];
			TileData copyTileData = (TileData)(GodotObject)copyTilesData[key];
			int pastePolygonsCount = pasteTileData.GetOccluderPolygonsCount(layer_id);
			int copyPolygonsCount = copyTileData.GetOccluderPolygonsCount(copyLayerID);
			for (int polygonIndex = 0; polygonIndex < pastePolygonsCount; polygonIndex++)
			{
				pasteTileData.RemoveOccluderPolygon(layer_id, 0);
			}
			for (int polygonIndex = 0; polygonIndex < copyPolygonsCount; polygonIndex++)
			{
				pasteTileData.AddOccluderPolygon(layer_id);
				pasteTileData.SetOccluderPolygon(layer_id, polygonIndex, copyTileData.GetOccluderPolygon(copyLayerID, polygonIndex));
			}
		}
	}

	private void OnButtonPressed(Node node)
	{
		var parent = node.GetParent();
		if (parent == null) return;
		foreach (Node child in parent.GetChildren().Cast<Node>())
		{
			if (child == node) continue;
			if (child is CanvasItem ci) ci.Visible = !ci.Visible;
		}
	}
}
