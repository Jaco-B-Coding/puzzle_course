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
		Callable.From(() => GameEvents.EmitBuildingPlaced(this)).CallDeferred();		// defined lambda functions that when called calls EmitBuildingPlaced funcitons --> Object type that wraps a C# functions, as they cannot be passed to Godot in raw form
	}

	public Vector2I GetGridCellPosition()
	{
		var gridPosition = GlobalPosition / 64;
		gridPosition = gridPosition.Floor();
		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}

}
