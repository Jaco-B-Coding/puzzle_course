using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Component;
using Godot;

// grid manager responsible for keeping track of all types thatt can be build and where and for the HighlightTileMapLayer to reflect avalid and updated buildings


namespace Game.Manager;

public partial class GridManager : Node
{
	private HashSet<Vector2I> ValidBuildableTiles = new();

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
		return ValidBuildableTiles.Contains(tilePosition);
	}


	public void HighlightTileBuildableTiles()
	{
		foreach (var tilePosition in ValidBuildableTiles)
		{
			highlightTilemapLayer.SetCell(tilePosition, 0, Vector2I.Zero); 
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

		for (var x = rootCell.X - buildingComponent.BuildableRadius; x <= rootCell.X + buildingComponent.BuildableRadius; x++)
		{
			for (var y = rootCell.Y - buildingComponent.BuildableRadius; y <= rootCell.Y + buildingComponent.BuildableRadius; y++)
			{
				var tilePosition = new Vector2I(x,y);
				if(!IsTilePositionValid(tilePosition)) continue;
				ValidBuildableTiles.Add(tilePosition);
			}
		}
		ValidBuildableTiles.Remove(buildingComponent.GetGridCellPosition());
	}

	private void OnBuildingPlaced(BuildingComponent buildingComponent)
	{
		UpdateValidBuildableTiles(buildingComponent);
	}
}
