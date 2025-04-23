using Game.Autoload;
using Godot;

namespace Game.Component;

public partial class BuildingComponent : Node2D
{
	[Export]
	public int BuildableRadius {get; private set;}   					// with semi colons you say that it is read only. Settable only within this class

	public override void _Ready()
	{
		AddToGroup(nameof(BuildingComponent)); 							// since already added here t the Group it is gonna be synchronized in any other call to it
		GameEvents.EmitBuildingPlaced(this);
	}

	public Vector2I GetGridCellPosition()
	{
		var gridPosition = GlobalPosition / 64;
		gridPosition = gridPosition.Floor();
		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}

}
