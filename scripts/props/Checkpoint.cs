using Godot;
using System;

public partial class Checkpoint : Area3D
{
	[Export] private GameManager _gameManager;
	[Export] private Marker3D RespawnMarker;
	[Export] private int _checkpointId = 0;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is CarController controller)
		{	
			_gameManager.UpdateCarCheckpoint(_checkpointId, controller);
			controller.RespawnPosition = RespawnMarker.GlobalPosition;
			controller.RespawnRotation = RespawnMarker.GlobalRotationDegrees;
		}
	}
}