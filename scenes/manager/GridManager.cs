using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Component;
using Godot;

// grid manager responsible for keeping track of all types thatt can be build and where and for the HighlightTileMapLayer to reflect avalid and updated buildings


namespace Game.Manager;

public partial class GridManager : Node
{
	private HashSet<Vector2I> occupiedCells = new(); 								// or new HashSet<Vector2>

	[Export]
	private TileMapLayer highlightTilemapLayer;	

	[Export]
	private TileMapLayer baseTerrainTilemapLayer;

    public override void _Ready()
    {
		var  GameEvents = GetNode<GameEvents>("/root/GameEvents");
    }

	public bool IsTilePositionValid(Vector2I tilePosition)	{
		var customData = baseTerrainTilemapLayer.GetCellTileData(tilePosition);

		if(customData == null) return false;
		if(!(bool)customData.GetCustomData("buildable")) return false;				// casting customdata as a bool, cause no access from here on its type and it could be many different ones

		return !occupiedCells.Contains(tilePosition);
	}

	public void MarkTileAsOccupied(Vector2I tilePosition)	{
		occupiedCells.Add(tilePosition);
	}

	public void HighlightTileBuildableTiles()
	{
		ClearHighlightedTiles();
		var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>();    //.Cast is a Linq method. iterates over nodes that are returned and casting all to buildingComponent for us  
		
		foreach (var buildingComponent in buildingComponents)
			{
				HighlightValidTilesInRadius(buildingComponent.GetGridCellPosition(), buildingComponent.BuildableRadius);	
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


	// private Methods
	private void HighlightValidTilesInRadius(Vector2I rootCell, int radius)	{

		for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x,y);
				if(!IsTilePositionValid(tilePosition)) continue;
				highlightTilemapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
			}
		}
		}
}
