using System;
using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Component;
using Game.Level.Util;
using Godot;

// grid manager responsible for keeping track of all types thatt can be build and where and for the HighlightTileMapLayer to reflect avalid and updated buildings


namespace Game.Manager;

public partial class GridManager : Node
{
	private const string IS_BUILDABLE = "is_buildable";
	private const string IS_WOOD = "is_wood";
	private const string IS_IGNORED = "is_ignored";

	[Signal]
	public delegate void ResourceTilesUpdatedEventHandler(int collectedTiles);
	[Signal]
	public delegate void GridStateUpdatedEventHandler();

	private HashSet<Vector2I> validBuildableTiles = new();
	private HashSet<Vector2I> collectedResourceTiles = new();
	private HashSet<Vector2I> occupiedTiles = new();

	[Export]
	private TileMapLayer highlightTilemapLayer;	
	[Export]
	private TileMapLayer baseTerrainTilemapLayer;

	private List<TileMapLayer> allTilemapLayers = new();
	private Dictionary<TileMapLayer, ElevationLayer> tileMapLayerToElevationLayer = new();

    public override void _Ready()
    {
		GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
		GameEvents.Instance.BuildingDestroyed += OnBuildingDestroyed;
		allTilemapLayers = GetAllTilemapLayers(baseTerrainTilemapLayer);
		MapTileMapLayersToElevationLayers();
    }


	public (TileMapLayer, bool) GetTileCustomData(Vector2I tilePosition, string dataName)	
	{
		foreach (var layer in allTilemapLayers)
		{
			var customData = layer.GetCellTileData(tilePosition);
			if(customData == null || (bool)customData.GetCustomData(IS_IGNORED)) continue;
			return (layer, (bool)customData.GetCustomData(dataName)) ;				// casting customdata as a bool, cause no access from here on its type and it could be many different ones	
		}
		return (null,false);
	}

	public bool IsTilePositionBuildable(Vector2I tilePosition) {
		return validBuildableTiles.Contains(tilePosition);
	}

	public bool IsTileAreaBuildable(Rect2I tileArea)
	{
		var tiles = tileArea.ToTiles(); 			// extension method to GoDot clas 

		if (tiles.Count == 0) return false;			// saftey check
		(TileMapLayer firsTileMapLayer, _) = GetTileCustomData(tiles[0], IS_BUILDABLE);			// getting the first tilemaplayer the tile belongs to; discard the second tuple value
		var targetElevationLayer = tileMapLayerToElevationLayer[firsTileMapLayer];

		return tiles.All((tilePosition) => 
		{
				(TileMapLayer tileMapLayer, bool is_buildable) =GetTileCustomData(tilePosition, IS_BUILDABLE);
				var elevationLayer = tileMapLayerToElevationLayer[tileMapLayer];
				return is_buildable && validBuildableTiles.Contains(tilePosition) && elevationLayer == targetElevationLayer;
		});
	}

	public void HighlightBuildableTiles()
	{
		foreach (var tilePosition in validBuildableTiles)
		{
			highlightTilemapLayer.SetCell(tilePosition, 0, Vector2I.Zero); 
		}
	}

	public void HighlightExpandedBuildableTiles(Rect2I tileArea, int radius)
	{
		var validTiles = GetValidTilesInRadius(tileArea, radius).ToHashSet();
		var expandedTiles = validTiles.Except(validBuildableTiles).Except(occupiedTiles); 
		var atlasCoords = new Vector2I(1,0);
		foreach (var tilePosition in expandedTiles)
			{
				highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords); 
			}
	}

	public void HighlightResourceTiles(Rect2I tileArea, int radius)
	{
		var resourceTiles = GetResourceTilesInRadius(tileArea, radius);
		var atlasCoords = new Vector2I(1,0);
		foreach (var tilePosition in resourceTiles )
			{
				highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords); 
			}
	}

	public void ClearHighlightedTiles()
		{
			highlightTilemapLayer.Clear();
		}

	
	// Helper Methods
	public Vector2I GetMouseGridCellPosition()
	{
		var mousePosition = highlightTilemapLayer.GetGlobalMousePosition();
		return ConvertWorldPositionToTilePosition(mousePosition);
	}

	public Vector2I ConvertWorldPositionToTilePosition(Vector2 worldPosition)
	{
		var tilePosition = worldPosition / 64;
		tilePosition = tilePosition.Floor();
		return new Vector2I((int)tilePosition.X, (int)tilePosition.Y);
	}

	private List<TileMapLayer> GetAllTilemapLayers(Node2D rootNode)
	{
		var result = new List<TileMapLayer>();									//using recursion in this method
		var children = rootNode.GetChildren();
		children.Reverse();

		foreach (var child in children)
		{
			if (child is Node2D childNode)
			{
				result.AddRange(GetAllTilemapLayers(childNode));				// here comes recursion --> goes down all childmaplayers to the last sub node and returns them one after the other
			}
		}

		if (rootNode is TileMapLayer tileMapLayer)
		{
		result.Add(tileMapLayer);			
		}
		return result;
	}

	private void MapTileMapLayersToElevationLayers()
	{
		foreach (var layer in allTilemapLayers)
		 {
			ElevationLayer elevationLayer;
			Node startNode = layer;
			do
			{
				var parent = startNode.GetParent();
				elevationLayer = parent as ElevationLayer;
				startNode = parent;
			} while (elevationLayer == null && startNode != null);

			tileMapLayerToElevationLayer[layer] = elevationLayer;
		 }
	}


	private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
	{
		occupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPositions());
		var rootCell = buildingComponent.GetGridCellPosition();
		var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);
		var validTiles = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius);
		validBuildableTiles.UnionWith(validTiles);
		validBuildableTiles.ExceptWith(occupiedTiles);									// removes occupied tiles from validBuildableTile Hashset
		EmitSignal(SignalName.GridStateUpdated);
	}

	private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent)
	{
		var rootCell = buildingComponent.GetGridCellPosition();
		var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);
		var resourceTiles = GetResourceTilesInRadius(tileArea, buildingComponent.BuildingResource.ResourceRadius);

		var oldResourceTileCount = collectedResourceTiles.Count;
		collectedResourceTiles.UnionWith(resourceTiles);

		if (oldResourceTileCount != collectedResourceTiles.Count)
		{
			EmitSignal(SignalName.ResourceTilesUpdated, collectedResourceTiles.Count);
		}
		EmitSignal(SignalName.GridStateUpdated);

	}
	
	private void RecalculateGrid(BuildingComponent exludeBuildingComponent)
	{
		occupiedTiles.Clear();
		validBuildableTiles.Clear();
		collectedResourceTiles.Clear();
		
		var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>()
		.Where((buildingComponent) => buildingComponent != exludeBuildingComponent);
		foreach (var buildingComponent in buildingComponents)
		{
		UpdateValidBuildableTiles(buildingComponent);			
		UpdateCollectedResourceTiles(buildingComponent);
		}

		EmitSignal(SignalName.ResourceTilesUpdated, collectedResourceTiles.Count);
		EmitSignal(SignalName.GridStateUpdated);
	}

	private bool IsTileInsideCircle(Vector2 centerPosition, Vector2 tilePosition, float radius)
	{
		var distanceX = centerPosition.X - tilePosition.X + .5;
		var distanceY = centerPosition.Y - tilePosition.Y + .5;
		var distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
		return distanceSquared <= radius*radius;
	}

	private List<Vector2I> GetTilesInRadius(Rect2I tileArea, int radius, Func<Vector2I, bool> filterFn)   // passing function as filter functionIS_BUILDABLE which returns boolean and accepts in this case a Vector2I as input
	{
		var result = new List<Vector2I>();
		var tileAreaF = tileArea.ToRect2F();			// we need to change the Rect2I to Rect2, as GetCenter chops of decimals for integer values
		var tileAreaCenter = tileAreaF.GetCenter();
		var radiusMod = Mathf.Max(tileAreaF.Size.X, tileAreaF.Size.Y)/2;

		for (var x = tileArea.Position.X - radius; x < tileArea.End.X + radius; x++)
		{
			for (var y = tileArea.Position.Y - radius; y < tileArea.End.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x,y);
				if(!IsTileInsideCircle(tileAreaCenter, tilePosition, radius + radiusMod) || !filterFn(tilePosition)) continue;
				result.Add(tilePosition);
			}
		}
		return result;	
	}

	private List<Vector2I> GetValidTilesInRadius(Rect2I tileArea, int radius)
	{
		return GetTilesInRadius( tileArea, radius, (tilePosition) => 
		{
			return GetTileCustomData(tilePosition, IS_BUILDABLE).Item2;
		});
	}

	private List<Vector2I> GetResourceTilesInRadius(Rect2I tileArea, int radius)
	{
		return GetTilesInRadius( tileArea, radius, (tilePosition) => 
		{
			return GetTileCustomData(tilePosition, IS_WOOD).Item2;
		});
	}

	private void OnBuildingPlaced(BuildingComponent buildingComponent)
	{
		UpdateValidBuildableTiles(buildingComponent);
		UpdateCollectedResourceTiles(buildingComponent);
	}

	private void OnBuildingDestroyed(BuildingComponent buildingComponent)
	{
		RecalculateGrid(buildingComponent);

	}
}
