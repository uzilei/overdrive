using Godot;
using System;

public partial class NitroTimerBar : ProgressBar
{
	[Export] private CarController _vehicle;
	[Export] private float _smoothingSpeed = 10.0f;

	public const float FillDuration = 0.6f;
	public const float DepleteAt = 1.8f;

	private float _timer = 0.0f;
	private bool _yellowActive = false;
	private bool _resetting = false;
	private float _smoothedValue = 0.0f;

	private StyleBoxFlat _bgStyleBoxCached;
	private Color _currentBgColor = new(1.0f, 1.0f, 0.0f);
	private Color _targetBgColor = new(1.0f, 1.0f, 0.0f);

	private float ApplyExponentialSmoothing(float current, float target, float speed, float delta)
	{
		float alpha = 1.0f - Mathf.Exp(-speed * (float)delta);
		return current + (target - current) * alpha;
	}

	public override void _Ready()
	{
		if (MaxValue <= 0)
			MaxValue = 1.0;
		Value = 0.0;

		StyleBox existingBg = GetThemeStylebox("background");
		if (existingBg is StyleBoxFlat sbfb)
			_bgStyleBoxCached = (StyleBoxFlat)sbfb.Duplicate(true);
		else
			_bgStyleBoxCached = new StyleBoxFlat();

		AddThemeStyleboxOverride("background", _bgStyleBoxCached);

		try { _currentBgColor = _bgStyleBoxCached.BgColor; } catch { _currentBgColor = _targetBgColor; }
	}

	public override void _Process(double delta)
	{
		int phase = -1;
		if (_vehicle == null)
		{
			if (!_resetting)
			{
				_resetting = true;
			}
		}
		else
		{
			phase = _vehicle.NitroPhaseLevel;
		}
		if (phase == 1)
		{
			if (!_yellowActive)
			{
				_yellowActive = true;
				_timer = 0.0f;
				_smoothedValue = 0.0f;
			}
			_timer += (float)delta;

			if (_timer <= FillDuration)
			{
				float progress = _timer / FillDuration;
				float target = Mathf.Lerp(0f, (float)MaxValue, (float)progress);
				Value = target;
				_smoothedValue = target;
				return;
			}

			if (_timer < DepleteAt)
			{
				Value = MaxValue;
				_smoothedValue = (float)MaxValue;
				return;
			}

			_smoothedValue = ApplyExponentialSmoothing(_smoothedValue, (float)MinValue, _smoothingSpeed, (float)delta);
			Value = _smoothedValue;
		}
		else
		{
			if (_yellowActive)
			{
				_yellowActive = false;
				_timer = 0.0f;
				_resetting = true;
			}
			if (_resetting)
			{
				_smoothedValue = ApplyExponentialSmoothing(_smoothedValue, (float)MinValue, _smoothingSpeed, (float)delta);
				Value = _smoothedValue;
			}
			else
			{
				Value = MinValue;
			}
		}

		float nitroRemaining = _vehicle.NitroRemaining;
		bool purpleEligible = _vehicle.PurpleEligible;

		if (nitroRemaining >= 0.001)
		{
            _targetBgColor = phase switch
            {
                3 => new Color(1.0f, 0.5f, 0.0f),
                2 => new Color(0.0f, 0.5f, 1.0f),
                4 => new Color(0.75f, 0.0f, 1.0f),
                _ => purpleEligible ? new Color(0.75f, 0.0f, 1.0f) : new Color(1.0f, 0.5f, 0.0f),
            };
        }
		else _targetBgColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
		
		_currentBgColor = new Color(
			(float)ApplyExponentialSmoothing(_currentBgColor.R, _targetBgColor.R, _smoothingSpeed, (float)delta),
			(float)ApplyExponentialSmoothing(_currentBgColor.G, _targetBgColor.G, _smoothingSpeed, (float)delta),
			(float)ApplyExponentialSmoothing(_currentBgColor.B, _targetBgColor.B, _smoothingSpeed, (float)delta),
			(float)ApplyExponentialSmoothing(_currentBgColor.A, _targetBgColor.A, _smoothingSpeed, (float)delta)
			);

		try { _bgStyleBoxCached.SetBgColor(_currentBgColor); } catch { try { _bgStyleBoxCached.BgColor = _currentBgColor; } catch { } }
	}
}
