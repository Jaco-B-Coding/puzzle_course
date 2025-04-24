using Game.Manager;
using Game.Resources.Building;
using Godot;

namespace Game;

public partial class Main : Node
{
	private GridManager gridManager;
	private Sprite2D cursor;
	private BuildingResource towerResource;
	private BuildingResource villageResource;
	private Button placeTowerButton;
	private Button placeVillageButton;
	private Node2D ySortRoot;

	private Vector2I? hoveredGridCell;
	private BuildingResource toPlaceBuildingResource;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		towerResource = GD.Load<BuildingResource>("res://resources/building/tower.tres");
		villageResource = GD.Load<BuildingResource>("res://resources/building/village.tres");
		gridManager = GetNode<GridManager>("GridManager");
		cursor = GetNode<Sprite2D>("Cursor");
		placeTowerButton = GetNode<Button>("PlaceTowerButton");
		placeVillageButton = GetNode<Button>("PlaceVillageButton");
		ySortRoot = GetNode<Node2D>("YSortRoot");

		cursor.Visible = false;

		placeTowerButton.Pressed += OnPlacedTowerButtonPressed;
		placeVillageButton.Pressed += OnPlacedVillageButtonPressed;

	}

	public override void _UnhandledInput(InputEvent evt)
	{
		if (hoveredGridCell.HasValue && evt.IsActionPressed("left_click") && gridManager.IsTilePositionBuildable(hoveredGridCell.Value))
		{
			PlaceBuildingAtHoveredCellPosition();
			cursor.Visible = false;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		var gridPosition = gridManager.GetMouseGridCellPosition();
		cursor.GlobalPosition = gridPosition * 64;
		if (toPlaceBuildingResource != null && cursor.Visible && (!hoveredGridCell.HasValue || hoveredGridCell.Value != gridPosition))
		{
			hoveredGridCell = gridPosition;
			gridManager.HighlightExpandedBuildableTiles(hoveredGridCell.Value, toPlaceBuildingResource.BuildableRadius);
		}
	}

	private void PlaceBuildingAtHoveredCellPosition()
	{
		if (!hoveredGridCell.HasValue) return;

		var building = toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
		ySortRoot.AddChild(building);

		building.GlobalPosition = hoveredGridCell.Value * 64;

		hoveredGridCell = null;
		gridManager.ClearHighlightedTiles();
	}


	private void OnPlacedTowerButtonPressed()
	{
		toPlaceBuildingResource = towerResource;
		cursor.Visible = true;
		gridManager.HighlightBuildableTiles();
	}

	private void OnPlacedVillageButtonPressed() {
		toPlaceBuildingResource = villageResource;
		cursor.Visible = true;
		gridManager.HighlightBuildableTiles();

	}

}
