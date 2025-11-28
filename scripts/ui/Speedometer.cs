using Godot;
using System;

public partial class Speedometer : Label
{
	[Export] private RigidBody3D _vehicle;

	[Export] private float _hysteresisKmh = 0.6f;

	private int _lastDisplay = 0;

	public override void _Process(double delta)
    {
		float rawKmh = _vehicle.LinearVelocity.Length() * 4.0f;

		if (Mathf.Abs(rawKmh - _lastDisplay) >= _hysteresisKmh)
		{
			_lastDisplay = (int)Mathf.Round(rawKmh);
		}

		Text = _lastDisplay.ToString();
    }
}