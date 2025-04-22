using System.Collections.Generic;
using Godot;

// grid manager responsible for keeping track of all types thatt can be build and where and for the HighlightTileMapLayer to reflect avalid and updated buildings


namespace Game.Manager;

public partial class GridManager : Node
{
	private HashSet<Vector2> occupiedCells = new(); 								// or new HashSet<Vector2>

	[Export]
	private TileMapLayer highlightTilemapLayer;	

	[Export]
	private TileMapLayer baseTerrainTilemapLayer;

	public override void _Ready()
	{
	}

	public bool IsTilePositionValid(Vector2 tilePosition)	{
		return !occupiedCells.Contains(tilePosition);
	}

	public void MarkTileAsOccupied(Vector2 tilePosition)	{
		occupiedCells.Add(tilePosition);
	}

	public void HighlightValidTilesInRadius(Vector2 rootCell, int radius)	{
		ClearHighlightedTiles();

		for (var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			for (var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				if(!IsTilePositionValid(new Vector2(x,y))) continue;
				highlightTilemapLayer.SetCell(new Vector2I((int)x, (int)y), 0, Vector2I.Zero);
			}
		}
		}

	public void ClearHighlightedTiles()
		{
			highlightTilemapLayer.Clear();
		}

	
	// Helper Functions
	public Vector2 GetMouseGridCellPosition()
	{
		var mousePosition = highlightTilemapLayer.GetGlobalMousePosition();
		var gridPosition = mousePosition / 64;
		gridPosition = gridPosition.Floor();
		return gridPosition;
	}
}
