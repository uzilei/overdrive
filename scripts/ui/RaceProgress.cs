using Godot;
using System;

public partial class RaceProgress : ProgressBar
{
    public override void _Ready()
    {
        Rounded = true;
		Value = 0;
    }

	public void UpdateValue(int checkpointId, int maxCheckpoints)
	{
		double percent = (double)checkpointId / maxCheckpoints * 100.0;
		Value = Math.Clamp(percent, 0.0, 100.0);
	}
}
