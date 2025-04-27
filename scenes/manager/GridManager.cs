using System;
using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Component;
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

    public override void _Ready()
    {
		GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
		GameEvents.Instance.BuildingDestroyed += OnBuildingDestroyed;
		allTilemapLayers = GetAllTilemapLayers(baseTerrainTilemapLayer);
    }


	public bool TileHasCustomData(Vector2I tilePosition, string dataName)	
	{
		foreach (var layer in allTilemapLayers)
		{
			var customData = layer.GetCellTileData(tilePosition);
			if(customData == null || (bool)customData.GetCustomData(IS_IGNORED)) continue;
			return (bool)customData.GetCustomData(dataName) ;				// casting customdata as a bool, cause no access from here on its type and it could be many different ones	
		}
		return false;
	}

	public bool IsTilePositionBuildable(Vector2I tilePosition) {
		return validBuildableTiles.Contains(tilePosition);
	}

	public void HighlightBuildableTiles()
	{
		foreach (var tilePosition in validBuildableTiles)
		{
			highlightTilemapLayer.SetCell(tilePosition, 0, Vector2I.Zero); 
		}
	}

	public void HighlightExpandedBuildableTiles(Vector2I rootCell, int radius)
	{
		var validTiles = GetValidTilesInRadius(rootCell, radius).ToHashSet();
		var expandedTiles = validTiles.Except(validBuildableTiles).Except(occupiedTiles); 
		var atlasCoords = new Vector2I(1,0);
		foreach (var tilePosition in expandedTiles)
			{
				highlightTilemapLayer.SetCell(tilePosition, 0, atlasCoords); 
			}
	}

	public void HighlightResourceTiles(Vector2I rootCell, int radius)
	{
		var resourceTiles = GetResourceTilesInRadius(rootCell, radius);
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

	private List<TileMapLayer> GetAllTilemapLayers(TileMapLayer rootTileMapLayer)
	{
		var result = new List<TileMapLayer>();									//using recursion in this method
		var children = rootTileMapLayer.GetChildren();
		children.Reverse();

		foreach (var child in children)
		{
			if (child is TileMapLayer childLayer)
			{
				result.AddRange(GetAllTilemapLayers(childLayer));				// here comes recursion --> goes down all childmaplayers to the last sub node and returns them one after the other
			}
		}
		result.Add(rootTileMapLayer);
		return result;
	}


	private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
	{
		occupiedTiles.Add(buildingComponent.GetGridCellPosition());
		var rootCell = buildingComponent.GetGridCellPosition();
		var validTiles = GetValidTilesInRadius(rootCell, buildingComponent.BuildingResource.BuildableRadius);
		validBuildableTiles.UnionWith(validTiles);
		validBuildableTiles.ExceptWith(occupiedTiles);									// removes occupied tiles from validBuildableTile Hashset
		EmitSignal(SignalName.GridStateUpdated);
	}

	private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent)
	{
		var rootCell = buildingComponent.GetGridCellPosition();
		var resourceTiles = GetResourceTilesInRadius(rootCell, buildingComponent.BuildingResource.ResourceRadius);

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


	private List<Vector2I> GetTilesInRadius(Vector2I rootCell, int radius, Func<Vector2I, bool> filterFn)   // passing function as filter functionIS_BUILDABLE which returns boolean and accepts in this case a Vector2I as input
	{
		var result = new List<Vector2I>();

		for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x,y);
				if(!filterFn(tilePosition)) continue;
				result.Add(tilePosition);
			}
		}
		return result;	
	}

	private List<Vector2I> GetValidTilesInRadius(Vector2I rootCell, int radius)
	{
		return GetTilesInRadius( rootCell, radius, (tilePosition) => 
		{
			return TileHasCustomData(tilePosition, IS_BUILDABLE);
		});
	}

	private List<Vector2I> GetResourceTilesInRadius(Vector2I rootCell, int radius)
	{
		return GetTilesInRadius( rootCell, radius, (tilePosition) => 
		{
			return TileHasCustomData(tilePosition, IS_WOOD);
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
