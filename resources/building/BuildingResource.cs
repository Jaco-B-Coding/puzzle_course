using Godot;

namespace Game.Resources.Building;

[GlobalClass]														// adds global class that we can access from the GoDot directory/editor
public partial class BuildingResource : Resource 					// extend to resource so we have a custom GoDot resource declared here
{
	[Export]
	public string DisplayName { get; private set; }
	[Export]
	public Vector2I Dimensions { get; private set; } = Vector2I.One;
	[Export]
	public int ResourceCost { get; private set; }
	[Export]
	public int BuildableRadius { get; private set;	}
	[Export]
	public int ResourceRadius { get; private set; }
	[Export]
	public PackedScene BuildingScene { get; private set; }
	[Export]
	public PackedScene SpriteScene {get ; private set;}
}
