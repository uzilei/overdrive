using Godot;
using System;

public partial class StartButton : Button
{
	public override void _Ready()
    {
		Connect("pressed", new Callable(this, nameof(OnPressed)));
		GrabFocus();
    }

	private void OnPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/CarSelect.tscn");
	}
}
