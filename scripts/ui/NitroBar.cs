using Godot;
using System;

public partial class NitroBar : ProgressBar
{
	[Export] private CarController _vehicle { get; set; }
	[Export] private float _smoothingSpeed = 10.0f;

	private float _smoothedValue = 0.0f;
	private StyleBoxFlat _fillStyleBoxCached;
	private int _lastNitroPhase = -1;
	private Color _currentFillColor = new(1.0f, 1.0f, 0.0f);
	private Color _targetColor;

	public override void _Ready()
	{
		StyleBox existing = GetThemeStylebox("fill");
		if (existing is StyleBoxFlat sbf)
			_fillStyleBoxCached = (StyleBoxFlat)sbf.Duplicate(true);
		else
			_fillStyleBoxCached = new StyleBoxFlat();

		AddThemeStyleboxOverride("fill", _fillStyleBoxCached);

		try { _currentFillColor = _fillStyleBoxCached.BgColor; } catch { _currentFillColor = _targetColor; }
	}
	
	public override void _Process(double delta)
	{
		float target = _vehicle.NitroRemaining;

		float alpha = (float)(1.0 - Mathf.Exp(-_smoothingSpeed * delta));
		_smoothedValue += (target - _smoothedValue) * alpha;

		Value = _smoothedValue;

		int phase = _vehicle.NitroPhaseLevel;

		_targetColor = phase switch
		{
			3 => new Color(1.0f, 0.5f, 0.0f),
			2 => new Color(0.0f, 0.5f, 1.0f),
			4 => new Color(0.75f, 0.0f, 1.0f),
			_ => new Color(1.0f, 1.0f, 0.0f),
		};
		_currentFillColor = new Color(
			_currentFillColor.R + (_targetColor.R - _currentFillColor.R) * alpha,
			_currentFillColor.G + (_targetColor.G - _currentFillColor.G) * alpha,
			_currentFillColor.B + (_targetColor.B - _currentFillColor.B) * alpha,
			_currentFillColor.A + (_targetColor.A - _currentFillColor.A) * alpha);

		try { _fillStyleBoxCached.SetBgColor(_currentFillColor); } catch { try { _fillStyleBoxCached.BgColor = _currentFillColor; } catch { } }

		_lastNitroPhase = phase;
	}
}
