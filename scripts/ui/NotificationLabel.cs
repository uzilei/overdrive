using Godot;
using System;

public partial class NotificationLabel : Label
{
	public override void _Ready()
	{
		Text = "";
	}

	public async void ShowLapsRemaining(int laps)
	{
		if (laps <= 0)
			Text = "FINITO";
		else if (laps == 1)
			Text = "1 GIRO RIMANENTE";
		else
			Text = $"{laps} GIRI RIMANENTI";

		await ToSignal(GetTree().CreateTimer(4.0f), "timeout");
		if (Text != "FINITO")
			Text = "";
	}
}