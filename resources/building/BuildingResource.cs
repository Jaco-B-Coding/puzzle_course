using Godot;

namespace Game.Resources.Building;

[GlobalClass]														// adds global class that we can access from the GoDot directory/editor
public partial class BuildingResource : Resource 					// extend to resource so we have a custom GoDot resource declared here
{
	[Export]
	public int BuildableRadius { get; private set;	}
	[Export]
	public int ResourceRadius { get; private set; }
	[Export]
	public PackedScene BuildingScene { get; private set; }
}
