using Game.Component;
using Godot;

namespace Game.Autoload;

public partial class GameEvents : Node
{
	public static GameEvents Instance { get; private set; }

	[Signal]						// defining a custom signal in GoDot
	public delegate void BuildingPlacedEventHandler(BuildingComponent buildingComponent);       // "EventHandler is a GoDot requirement for custom Events

    public override void _Notification(int what)
    {
       if (what == NotificationSceneInstantiated)
	    {
			Instance = this;
		}
    }


	public static void EmitBuildingPlaced(BuildingComponent buildingComponent)
	 {
		Instance.EmitSignal(SignalName.BuildingPlaced, buildingComponent);
	 }
}
