using Godot;
using System;

public partial class MatchSettings : Control
{
	[Export] private Button _startButton;

	[ExportGroup("Red")]
	[Export] private Button _redSedanButton;
	[Export] private Button _redSuvButton;
	[Export] private Button _redMuscleButton;
	[Export] private Button _redSuperButton;
	[Export] private Button _redPoliceButton;
	[Export] private Button _redNobodyButton;

	[ExportGroup("Blue")]
	[Export] private Button _blueSedanButton;
	[Export] private Button _blueSuvButton;
	[Export] private Button _blueMuscleButton;
	[Export] private Button _blueSuperButton;
	[Export] private Button _bluePoliceButton;
	[Export] private Button _blueNobodyButton;

	[ExportGroup("Green")]
	[Export] private Button _greenSedanButton;
	[Export] private Button _greenSuvButton;
	[Export] private Button _greenMuscleButton;
	[Export] private Button _greenSuperButton;
	[Export] private Button _greenPoliceButton;
	[Export] private Button _greenNobodyButton;

	[ExportGroup("Orange")]
	[Export] private Button _orangeSedanButton;
	[Export] private Button _orangeSuvButton;
	[Export] private Button _orangeMuscleButton;
	[Export] private Button _orangeSuperButton;
	[Export] private Button _orangePoliceButton;
	[Export] private Button _orangeNobodyButton;

	[ExportGroup("Texts")]
	[Export] private Label _nameRed;
	[Export] private Label _nameBlue;
	[Export] private Label _nameGreen;
	[Export] private Label _nameOrange;

	[ExportGroup("Slider")]
	[Export] private Label _lapsLabel;
	[Export] private HSlider _lapsSlider;

	private enum CarId { NoCar = 0, Sedan = 1, Suv = 2, Muscle = 3, Super = 4, Police = 5 }

	private CarId _selectedRed = CarId.NoCar;
	private CarId _selectedBlue = CarId.NoCar;
	private CarId _selectedGreen = CarId.NoCar;
	private CarId _selectedOrange = CarId.NoCar;

	private string _redSedanPath = "res://scenes/prefabs/cars/red/SedanRed.tscn";
	private string _redSuvPath = "res://scenes/prefabs/cars/red/SuvRed.tscn";
	private string _redMusclePath = "res://scenes/prefabs/cars/red/MuscleRed.tscn";
	private string _redSuperPath = "res://scenes/prefabs/cars/red/SuperRed.tscn";
	private string _redPolicePath = "res://scenes/prefabs/cars/red/PoliceRed.tscn";

	private string _blueSedanPath = "res://scenes/prefabs/cars/blue/SedanBlue.tscn";
	private string _blueSuvPath = "res://scenes/prefabs/cars/blue/SuvBlue.tscn";
	private string _blueMusclePath = "res://scenes/prefabs/cars/blue/MuscleBlue.tscn";
	private string _blueSuperPath = "res://scenes/prefabs/cars/blue/SuperBlue.tscn";
	private string _bluePolicePath = "res://scenes/prefabs/cars/blue/PoliceBlue.tscn";

	private string _greenSedanPath = "res://scenes/prefabs/cars/green/SedanGreen.tscn";
	private string _greenSuvPath = "res://scenes/prefabs/cars/green/SuvGreen.tscn";
	private string _greenMusclePath = "res://scenes/prefabs/cars/green/MuscleGreen.tscn";
	private string _greenSuperPath = "res://scenes/prefabs/cars/green/SuperGreen.tscn";
	private string _greenPolicePath = "res://scenes/prefabs/cars/green/PoliceGreen.tscn";

	private string _orangeSedanPath = "res://scenes/prefabs/cars/orange/SedanOrange.tscn";
	private string _orangeSuvPath = "res://scenes/prefabs/cars/orange/SuvOrange.tscn";
	private string _orangeMusclePath = "res://scenes/prefabs/cars/orange/MuscleOrange.tscn";
	private string _orangeSuperPath = "res://scenes/prefabs/cars/orange/SuperOrange.tscn";
	private string _orangePolicePath = "res://scenes/prefabs/cars/orange/PoliceOrange.tscn";

	private PackedScene _gameScenePacked = GD.Load<PackedScene>("res://scenes/Game.tscn");
	private Node _pendingGameInstance = null;

	public int SelectedRedIndex => (int)_selectedRed;
	public int SelectedBlueIndex => (int)_selectedBlue;
	public int SelectedGreenIndex => (int)_selectedGreen;
	public int SelectedOrangeIndex => (int)_selectedOrange;

	public override void _Ready()
	{
		RegisterButton(_redSedanButton, _nameRed, CarId.Sedan, "RIVALE");
		RegisterButton(_redSuvButton, _nameRed, CarId.Suv, "TITANIO");
		RegisterButton(_redMuscleButton, _nameRed, CarId.Muscle, "TEMPESTA");
		RegisterButton(_redSuperButton, _nameRed, CarId.Super, "HURACÁN");
		RegisterButton(_redPoliceButton, _nameRed, CarId.Police, "SCERIFFO");
		RegisterButton(_redNobodyButton, _nameRed, CarId.NoCar, "Nessuno");

		RegisterButton(_blueSedanButton, _nameBlue, CarId.Sedan, "RIVALE");
		RegisterButton(_blueSuvButton, _nameBlue, CarId.Suv, "TITANIO");
		RegisterButton(_blueMuscleButton, _nameBlue, CarId.Muscle, "TEMPESTA");
		RegisterButton(_blueSuperButton, _nameBlue, CarId.Super, "HURACÁN");
		RegisterButton(_bluePoliceButton, _nameBlue, CarId.Police, "SCERIFFO");
		RegisterButton(_blueNobodyButton, _nameBlue, CarId.NoCar, "Nessuno");

		RegisterButton(_greenSedanButton, _nameGreen, CarId.Sedan, "RIVALE");
		RegisterButton(_greenSuvButton, _nameGreen, CarId.Suv, "TITANIO");
		RegisterButton(_greenMuscleButton, _nameGreen, CarId.Muscle, "TEMPESTA");
		RegisterButton(_greenSuperButton, _nameGreen, CarId.Super, "HURACÁN");
		RegisterButton(_greenPoliceButton, _nameGreen, CarId.Police, "SCERIFFO");
		RegisterButton(_greenNobodyButton, _nameGreen, CarId.NoCar, "Nessuno");

		RegisterButton(_orangeSedanButton, _nameOrange, CarId.Sedan, "RIVALE");
		RegisterButton(_orangeSuvButton, _nameOrange, CarId.Suv, "TITANIO");
		RegisterButton(_orangeMuscleButton, _nameOrange, CarId.Muscle, "TEMPESTA");
		RegisterButton(_orangeSuperButton, _nameOrange, CarId.Super, "HURACÁN");
		RegisterButton(_orangePoliceButton, _nameOrange, CarId.Police, "SCERIFFO");
		RegisterButton(_orangeNobodyButton, _nameOrange, CarId.NoCar, "Nessuno");

		_redSedanButton.GrabFocus();

		UpdateStartButtonState();

		if (_startButton != null)
		{
			_startButton.Pressed += () => SpawnSelectionsAndConfirm();
		}

		if (_lapsSlider != null && _lapsLabel != null)
		{
			int initial = (int)_lapsSlider.Value;
			_lapsLabel.Text = (initial == 1) ? "1 GIRO" : $"{initial} GIRI";

			_lapsSlider.ValueChanged += (double val) =>
			{
				int v = (int)val;
				_lapsLabel.Text = (v == 1) ? "1 GIRO" : $"{v} GIRI";
			};
		}
	}

	private void BeginStart()
	{
		if (_pendingGameInstance != null)
			return;

		if (_gameScenePacked == null)
			_gameScenePacked = GD.Load<PackedScene>("res://scenes/Game.tscn");

		_pendingGameInstance = _gameScenePacked.Instantiate();
	}

	private void ConfirmStart()
	{
		if (_pendingGameInstance == null)
			return;

		var parent = GetParent();
		if (parent != null)
		{
			parent.AddChild(_pendingGameInstance);
			_pendingGameInstance.Name = "Game";
			Hide();
			_pendingGameInstance = null;
		}
	}

	private void SpawnSelectionsAndConfirm()
	{
		if (_pendingGameInstance == null)
			BeginStart();

		if (_pendingGameInstance == null)
			return;

		CarId[] order = [_selectedRed, _selectedBlue, _selectedGreen, _selectedOrange];
		int count = 0;
		for (int i = 0; i < order.Length; i++)
		{
			if (order[i] == CarId.NoCar)
				break;
			count++;
		}

		if (count == 0)
		{
			GD.PrintErr("MatchSettings: no cars selected — aborting start.");
			ConfirmStart();
			return;
		}

		string GetPathFor(int colorIndex, CarId id)
		{
			switch (colorIndex)
			{
				case 0:
					switch (id)
					{
						case CarId.Sedan: return _redSedanPath;
						case CarId.Suv: return _redSuvPath;
						case CarId.Muscle: return _redMusclePath;
						case CarId.Super: return _redSuperPath;
						case CarId.Police: return _redPolicePath;
						default: return null;
					}
				case 1:
					switch (id)
					{
						case CarId.Sedan: return _blueSedanPath;
						case CarId.Suv: return _blueSuvPath;
						case CarId.Muscle: return _blueMusclePath;
						case CarId.Super: return _blueSuperPath;
						case CarId.Police: return _bluePolicePath;
						default: return null;
					}
				case 2:
					switch (id)
					{
						case CarId.Sedan: return _greenSedanPath;
						case CarId.Suv: return _greenSuvPath;
						case CarId.Muscle: return _greenMusclePath;
						case CarId.Super: return _greenSuperPath;
						case CarId.Police: return _greenPolicePath;
						default: return null;
					}
				case 3:
					switch (id)
					{
						case CarId.Sedan: return _orangeSedanPath;
						case CarId.Suv: return _orangeSuvPath;
						case CarId.Muscle: return _orangeMusclePath;
						case CarId.Super: return _orangeSuperPath;
						case CarId.Police: return _orangePolicePath;
						default: return null;
					}
				default: return null;
			}
		}

        string[] targets;
        switch (count)
		{
			case 1:
				targets = ["View1Player/SubViewportContainer1/SubViewport1Player"];
				break;
			case 2:
				targets = [
					"View2Players/GridContainer/SubViewportContainer1/SubViewport2PlayersRed",
					"View2Players/GridContainer/SubViewportContainer2/SubViewport2PlayersBlue"
				];
				break;
			case 3:
				targets = [
					"View3Players/VBoxContainer/SubViewportContainer1/SubViewport3PlayersRed",
					"View3Players/VBoxContainer/HBoxContainer/SubViewportContainer2/SubViewport3PlayersBlue",
					"View3Players/VBoxContainer/HBoxContainer/SubViewportContainer3/SubViewport3PlayersGreen"
				];
				break;
			default:
				targets = [
					"View4Players/GridContainer/SubViewportContainer1/SubViewport4PlayersRed",
					"View4Players/GridContainer/SubViewportContainer2/SubViewport4PlayersBlue",
					"View4Players/GridContainer/SubViewportContainer3/SubViewport4PlayersGreen",
					"View4Players/GridContainer/SubViewportContainer4/SubViewport4PlayersOrange"
				];
				break;
		}

		for (int i = 1; i <= 4; i++)
		{
			string[] names = [$"View{i}Players", $"View{i}Player"];
			foreach (var nm in names)
			{
				var viewNode = _pendingGameInstance.GetNodeOrNull<CanvasItem>(nm);
				if (viewNode != null)
					viewNode.Visible = (count == i);
			}
		}

		for (int i = 0; i < count && i < targets.Length; i++)
		{
			string targetPath = targets[i];
			var targetNode = _pendingGameInstance.GetNodeOrNull<Node>(targetPath);
			if (targetNode == null)
			{
				GD.PrintErr($"MatchSettings: target node '{targetPath}' not found (player count={count}).");
				continue;
			}

			var carId = order[i];
			string carPath = GetPathFor(i, carId);
			if (string.IsNullOrEmpty(carPath))
			{
				GD.PrintErr($"MatchSettings: no prefab path for color index {i} car id {carId}.");
				continue;
			}

			PackedScene packed = GD.Load<PackedScene>(carPath);
			if (packed == null)
			{
				GD.PrintErr($"MatchSettings: failed to load car prefab at '{carPath}'.");
				continue;
			}

			Node car = packed.Instantiate();
			car.Name = "car";
			targetNode.AddChild(car);
		}

		if (_lapsSlider != null)
		{
			int laps = (int)_lapsSlider.Value;
			var gm = FindGameManager(_pendingGameInstance);
			if (gm != null)
			{
				gm.MaxLaps = laps;
			}
		}

		ConfirmStart();
	}

	private GameManager FindGameManager(Node root)
	{
		if (root == null)
			return null;
		if (root is GameManager gm)
			return gm;
		foreach (Node child in root.GetChildren())
		{
			var found = FindGameManager(child);
			if (found != null)
				return found;
		}
		return null;
	}

	private void RegisterButton(Button btn, Label targetLabel, CarId car, string displayName)
	{
		if (btn == null || targetLabel == null)
			return;

		btn.Pressed += () =>
		{
			targetLabel.Text = displayName;

			if (targetLabel == _nameRed)
				_selectedRed = car;
			else if (targetLabel == _nameBlue)
				_selectedBlue = car;
			else if (targetLabel == _nameGreen)
				_selectedGreen = car;
			else if (targetLabel == _nameOrange)
				_selectedOrange = car;

			UpdateStartButtonState();
		};
	}

	private bool IsSelectionValid()
	{
		CarId[] order = [_selectedRed, _selectedBlue, _selectedGreen, _selectedOrange];

		int lastIndex = -1;
		for (int i = 0; i < order.Length; i++)
		{
			if (order[i] != CarId.NoCar)
				lastIndex = i;
		}
		if (lastIndex == -1)
			return false;

		for (int i = 0; i <= lastIndex; i++)
		{
			if (order[i] == CarId.NoCar)
				return false;
		}

		bool allPolice = true;
		for (int i = 0; i <= lastIndex; i++)
		{
			if (order[i] != CarId.Police)
			{
				allPolice = false;
				break;
			}
		}
		if (allPolice)
			return false;

		return true;
	}

	private void UpdateStartButtonState()
	{
		if (_startButton == null)
			return;
		_startButton.Disabled = !IsSelectionValid();
	}
}
