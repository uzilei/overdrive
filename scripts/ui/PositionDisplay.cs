using Godot;
using System;

public partial class PositionDisplay : Label
{
	public void UpdatePosition(int RacerPosition, int TotalRacers)
    {
        Text = $"{RacerPosition}/{TotalRacers}";
    }
}
