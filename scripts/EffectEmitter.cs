using Godot;
using System;

public partial class EffectEmitter : GpuParticles3D
{
	[Export] private bool _startEmitting = false;

	public override void _Ready()
	{
		Emitting = _startEmitting;
	}

	public void OnEmitOn()
	{
		Emitting = true;
	}

	public void OnEmitOff()
	{
		Emitting = false;
	}

	public void ToggleEmitting()
	{
		Emitting = !Emitting;
	}
}
