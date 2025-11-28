using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node3D
{
	[Export] private Label _countdownLabel;
	[Export] private int _countdownSeconds = 5;
	[Export] private int _maxCheckpoints = 2;

	private List<CarController> _registeredCars = new();
	private bool _endSequenceStarted = false;
	private bool _countdownFinished = false;

	public int MaxLaps = 10;

	public override void _Ready()
	{
		StartCountdown();
		RecalculatePositions();
	}

	public void RegisterCar(CarController car)
	{
		if (car == null)
			return;
		if (_registeredCars.Contains(car))
			return;
		if (_registeredCars.Count >= 4)
			return;
		
		_registeredCars.Add(car);
		car.Freeze = true;

		if (_countdownFinished)
			car.Freeze = false;
	}

	private async void StartCountdown()
	{
		for (int s = _countdownSeconds; s >= 1; s--)
		{
			_countdownLabel.Text = s.ToString();
			await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
		}

		_countdownFinished = true;
		foreach (var car in _registeredCars.Where(c => c.Team == 0))
		{
			car.Freeze = false;
			if (car.TimerUI != null)
				car.TimerUI.StartTimer = true;
		}
		_countdownLabel.Text = "GO!";
		await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
		_countdownLabel.Text = "";

		if (_registeredCars.Any(c => c.Team == 1))
		{
			for (int s = 4; s >= 1; s--)
			{
				_countdownLabel.Text = s.ToString();
				await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
			}
			foreach (var car in _registeredCars.Where(c => c.Team == 1))
			{
				car.Freeze = false;
				if (car.TimerUI != null)
					car.TimerUI.StartTimer = true;
			}
			_countdownLabel.Text = "GO!";
			await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
			_countdownLabel.Text = "";
		}
	}
	private Dictionary<int, int> _nextOrderForCheckpoint = new();
	private Dictionary<CarController, int> _carCheckpointOrder = new();
	private Dictionary<CarController, int> _carLaps = new();

	public void UpdateCarCheckpoint(int checkpointId, CarController controller)
	{
		int prev = controller.CurrentCheckpoint;

		controller.CurrentCheckpoint = checkpointId;

		if (prev == _maxCheckpoints && checkpointId == 1)
		{
			int laps = 0;
			_carLaps.TryGetValue(controller, out laps);
			laps++;
			if (laps > MaxLaps) laps = MaxLaps;
			_carLaps[controller] = laps;
			int remaining = Math.Max(0, MaxLaps - laps);
			if (controller.NotificationUI != null)
			{
				controller.NotificationUI.ShowLapsRemaining(remaining);
			}
			if (laps >= MaxLaps)
			{
				controller.RaceComplete = true;
				if (controller.TimerUI != null)
					controller.TimerUI.StartTimer = false;
				controller.Explode();
			}
		}
		int next = 0;
		if (!_nextOrderForCheckpoint.TryGetValue(checkpointId, out next))
			next = 0;
		_carCheckpointOrder[controller] = next;
		_nextOrderForCheckpoint[checkpointId] = next + 1;
		RecalculatePositions();
		controller.RaceProgressUI.UpdateValue(checkpointId, _maxCheckpoints);
		if (_registeredCars.Any(c => c.Team == 0) && _registeredCars.Where(c => c.Team == 0).All(c => c.RaceComplete))
		{
			foreach (var crim in _registeredCars.Where(c => c.Team == 1 && !c.RaceComplete))
			{
				if (!crim.Exploding)
					crim.Explode();
				crim.RaceComplete = true;
				if (crim.TimerUI != null)
					crim.TimerUI.StartTimer = false;
				if (crim.NotificationUI != null)
					crim.NotificationUI.ShowLapsRemaining(0);
			}
		}

		if (!_endSequenceStarted && _registeredCars.Count > 0 && _registeredCars.All(c => c.RaceComplete))
		{
			StartEndSequence();
		}
	}

	private async void StartEndSequence()
	{
		_endSequenceStarted = true;
		await ToSignal(GetTree().CreateTimer(3.0f), "timeout");
		_countdownLabel.Text = "Gara finita, resettiamo...";
	}

	private void RecalculatePositions()
	{
		int total = _registeredCars.Count;
		var ordered = _registeredCars
			.OrderByDescending(c => c.CurrentCheckpoint + (_carLaps.ContainsKey(c) ? _carLaps[c] * _maxCheckpoints : 0))
			.ThenBy(c => _carCheckpointOrder.ContainsKey(c) ? _carCheckpointOrder[c] : int.MaxValue)
			.ToList();
		for (int i = 0; i < ordered.Count; i++)
		{
			var car = ordered[i];
			if (car != null)
				car.PositionUI.UpdatePosition(i + 1, total);
				car.RacePosition = i + 1;
		}
	}
}
