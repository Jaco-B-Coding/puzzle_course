using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Resources.Building;
using Godot;

namespace Game.Component;

public partial class BuildingComponent : Node2D
{
	[Export(PropertyHint.File, "*.tres")]
	private string buildingResourcePath;

	public BuildingResource BuildingResource { get; private set; }

	private HashSet<Vector2I> occupiedTiles = new();

	public override void _Ready()
	{
		if (buildingResourcePath != null)
		{
			BuildingResource = GD.Load<BuildingResource>(buildingResourcePath);
		}
		AddToGroup(nameof(BuildingComponent)); 							// since already added here to the Group it is gonna be synchronized in any other call to it
		Callable.From(Initialize).CallDeferred();		// defined lambda functions that when called calls EmitBuildingPlaced funcitons --> Object type that wraps a C# functions, as they cannot be passed to Godot in raw form
	}

	public Vector2I GetGridCellPosition()
	{
		var gridPosition = GlobalPosition / 64;
		gridPosition = gridPosition.Floor();
		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}

	public HashSet<Vector2I> GetOccupiedCellPositions()
	{
		return occupiedTiles.ToHashSet(); 		// takes all of the elemets and creates a new hashset but doesnßt reference te same hashset, so no data can be modified from the outside
	}

	public bool IsTileInBuildingArea(Vector2I tilePosition)
	{
		return occupiedTiles.Contains(tilePosition);
	}

	public void Destroy()
	{
		GameEvents.EmitBuildingDestroyed(this);
		Owner.QueueFree(); 						//owner is the root node of the scene 
	}

	private void CalculateOccupiedCellPositions()
	{
		var gridPosition = GetGridCellPosition();
		for (int x = gridPosition.X; x < gridPosition.X + BuildingResource.Dimensions.X; x++)
		{
			for ( int y = gridPosition.Y; y < gridPosition.Y + BuildingResource.Dimensions.Y; y++)
			{
				occupiedTiles.Add(new Vector2I(x,y));
			}
		}
	}

	private void Initialize()
	{
		CalculateOccupiedCellPositions();
		GameEvents.EmitBuildingPlaced(this);
	}

}
