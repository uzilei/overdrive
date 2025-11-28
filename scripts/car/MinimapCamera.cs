using Godot;
using System;

public partial class MinimapCamera : Camera3D
{
	[Export] private CarController _vehicle; 
	[Export] private Vector3 _offset = new();

	public override void _Process(double delta)
    {
		GlobalPosition = _vehicle.GlobalPosition + _offset;
		var gr = GlobalRotation;
		gr.Y = _vehicle.GlobalRotation.Y;
		GlobalRotation = gr;
    }
}