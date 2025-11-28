using Godot;
using System;

public partial class NitroBottle : Area3D
{
	[Export] private float _radiansPerSecond = 4f;
	[Export] private int _nitroToAdd = 25;

	[Signal] public delegate void OnPickUpEventHandler();

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		var r = Rotation;
		r.Y += _radiansPerSecond * (float)delta;
		Rotation = r;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is CarController controller)
		{
			controller.OnNitroBottleEntered(_nitroToAdd);
			EmitSignal(SignalName.OnPickUp);
		}
	}
}