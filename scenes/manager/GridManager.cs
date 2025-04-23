using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Component;
using Godot;

// grid manager responsible for keeping track of all types thatt can be build and where and for the HighlightTileMapLayer to reflect avalid and updated buildings


namespace Game.Manager;

public partial class GridManager : Node
{
	private HashSet<Vector2I> validBuildableTiles = new();

	[Export]
	private TileMapLayer highlightTilemapLayer;	

	[Export]
	private TileMapLayer baseTerrainTilemapLayer;


    public override void _Ready()
    {
		GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
    }


	public bool IsTilePositionValid(Vector2I tilePosition)	{
		var customData = baseTerrainTilemapLayer.GetCellTileData(tilePosition);

		if(customData == null) return false;
		return (bool)customData.GetCustomData("buildable") ;				// casting customdata as a bool, cause no access from here on its type and it could be many different ones

	}

	public bool IsTilePositionBuildable(Vector2I tilePosition) {
		return validBuildableTiles.Contains(tilePosition);
	}


	public void HighlightTileBuildableTiles()
	{
		foreach (var tilePosition in validBuildableTiles)
		{
			highlightTilemapLayer.SetCell(tilePosition, 0, Vector2I.Zero); 
		}
	}

	public void HighlightExpandedBuildableTiles(Vector2I rootCell, int radius)
	{
		ClearHighlightedTiles();
		HighlightTileBuildableTiles();

		var validTiles = GetValidTilesInRadius(rootCell, radius).ToHashSet();
		var expandedTiles = validTiles.Except(validBuildableTiles).Except(GetOccupiedTiles()); 
		var atlasCoords = new Vector2I(1,0);
		foreach (var tilePosition in expandedTiles)
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
		var gridPosition = mousePosition / 64;
		gridPosition = gridPosition.Floor();
		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}


	private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
	{
		var rootCell = buildingComponent.GetGridCellPosition();
		var validTiles = GetValidTilesInRadius( rootCell, buildingComponent.BuildableRadius);
		validBuildableTiles.UnionWith(validTiles);


		validBuildableTiles.ExceptWith(GetOccupiedTiles());									// removes occupied tiles from validBuildableTile Hashset
	}

	private List<Vector2I> GetValidTilesInRadius(Vector2I rootCell, int radius)
	{
		var result = new List<Vector2I>();

		for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x,y);
				if(!IsTilePositionValid(tilePosition)) continue;
				result.Add(tilePosition);
			}
		}
		return result;	
	}

	private IEnumerable<Vector2I> GetOccupiedTiles() {
		var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>();
		var occupiedTiles = buildingComponents.Select(x => x.GetGridCellPosition());
		return occupiedTiles;
	}

	private void OnBuildingPlaced(BuildingComponent buildingComponent)
	{
		UpdateValidBuildableTiles(buildingComponent);
	}
}
