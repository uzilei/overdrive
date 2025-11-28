using Godot;
using System;

public partial class TimerLabel : Label
{
	[Export] public bool StartTimer = false;
	private double _elapsedSeconds = 0.0;

	public override void _Process(double delta)
	{
		if (!StartTimer) return;

		_elapsedSeconds += delta;
		UpdateText();
	}

	public void ResetTimer()
	{
		_elapsedSeconds = 0.0;
		UpdateText();
	}

	private void UpdateText()
	{
		int minutes = (int)(_elapsedSeconds / 60.0);
		int seconds = (int)(_elapsedSeconds % 60);
		int milliseconds = (int)((_elapsedSeconds - Mathf.Floor(_elapsedSeconds)) * 1000.0);

		Text = $"{minutes} : {seconds:00} : {milliseconds:000}";
	}
}
