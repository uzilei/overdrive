using Godot;
using System;

public partial class CameraController : Camera3D
{
    [Export] private float _distance = 6.5f;
    [Export] private float _height = 2.0f;
    [Export] private float _lookAtYOffset = 1.25f;

    [Export] private float _slopeYOffset = 5.0f;
    [Export] private float _slopeYSmooth = 2.0f;

    [Export] private float _brakeDistanceDecrease = 0.5f;
    [Export] private float _brakeFovDecrease = 2.0f;
    [Export] private float _cameraAdjustSmooth = 7.5f;
    [Export] private float _nitroDistanceIncreasePerPhase = 1.5f;
    [Export] private float _nitroFovIncreasePerPhase = 1.0f;
    [Export] private float _nitroJoltRecoverSpeed = 6.0f;

    [Export] private float _turnOffsetSmooth = 4.0f;
    [Export] private float _angularToOffset = 0.3f;
    [Export] private float _angularToTilt = 0.2f;
    [Export] private float _maxTiltDegrees = 4.0f;
    [Export] private float _tiltSmooth = 4.0f;

    private Transform3D _initialTransform;
    private float _baseFov;
    private float _desiredFov;
    private Vector3 _previousPosition;

    private float _currentTurnYaw = 0f;
    private float _currentDistance;
    private float _currentSlopeYOffset = 0f;
    private float _currentTilt = 0f;
    private int _prevNitroLevel = 0;
    private float _joltOffset = 0f;
    private float _joltTarget = 0f;


    public override void _Ready()
    {
        _initialTransform = Transform;
        _baseFov = Fov + 10;
        _desiredFov = _baseFov;
        _previousPosition = GlobalPosition;
        _currentDistance = _distance;
        _currentSlopeYOffset = 0f;
        UpdateCamera();
    }

    public override void _PhysicsProcess(double delta)
    {
        var parent = GetParent() as Node3D;
        Vector3 target = parent.GlobalTransform.Origin;
        Vector3 pos = GlobalTransform.Origin;

        var car = parent as CarController;
        bool reduceForBrakeOrDrift = (car != null) && (car.Drifting || car.Braking);
        float targetDistance = reduceForBrakeOrDrift ? Mathf.Max(0f, _distance - _brakeDistanceDecrease) : _distance;
        int nitroLevel = (car != null) ? car.NitroPhaseLevel : 0;

        if (nitroLevel > _prevNitroLevel)
        {
            _joltTarget = nitroLevel * _nitroDistanceIncreasePerPhase;
        }

        _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, Mathf.Min(1f, _cameraAdjustSmooth * (float)delta));

        Vector3 fromTarget = pos - target;
        Vector3 dir = fromTarget.LengthSquared() > 1e-6f ? fromTarget.Normalized() : -parent.GlobalTransform.Basis.Z;
        pos = target + dir * (_currentDistance + _joltOffset);

        float targetSlopeOffset = 0f;
        if (parent != null)
        {
            Vector3 carForward = -parent.GlobalTransform.Basis.Z;
            targetSlopeOffset = -carForward.Y * _slopeYOffset;
        }
        _currentSlopeYOffset = Mathf.Lerp(_currentSlopeYOffset, targetSlopeOffset, Mathf.Min(1f, _slopeYSmooth * (float)delta));
        pos.Y = target.Y + _height + _currentSlopeYOffset;

        var rbParent = parent as RigidBody3D;

        GlobalPosition = pos;

        float avY = rbParent.AngularVelocity.Y;
        float baseYaw = avY * _angularToOffset;

        float tYaw = Mathf.Min(1f, _turnOffsetSmooth * (float)delta);
        _currentTurnYaw = _currentTurnYaw + (baseYaw - _currentTurnYaw) * tYaw;

        float desiredTilt = avY * _angularToTilt;
        float maxTiltRad = MathF.PI * (_maxTiltDegrees / 180.0f);
        desiredTilt = Mathf.Clamp(desiredTilt, -maxTiltRad, maxTiltRad);
        float tTilt = Mathf.Min(1f, _tiltSmooth * (float)delta);
        _currentTilt += (desiredTilt - _currentTilt) * tTilt;

        LookAt(target + Vector3.Up * _lookAtYOffset, Vector3.Up);
        var rot = Rotation;
        rot.Y += _currentTurnYaw;
        rot.Z += _currentTilt;
        Rotation = rot;
        float targetFov = reduceForBrakeOrDrift ? Mathf.Max(1f, _baseFov - _brakeFovDecrease) : _baseFov;
        if (car != null)
        {
            int nl = car.NitroPhaseLevel;
            if (nl > 0)
                targetFov += nl * _nitroFovIncreasePerPhase;
            _prevNitroLevel = nl;
        }

        float joltLerp = Mathf.Min(1f, _nitroJoltRecoverSpeed * (float)delta);
        _joltOffset = Mathf.Lerp(_joltOffset, _joltTarget, joltLerp);
        _joltTarget = Mathf.Lerp(_joltTarget, 0f, joltLerp);
        Fov = Mathf.Lerp(Fov, targetFov, Mathf.Min(1f, _cameraAdjustSmooth * (float)delta));

        _previousPosition = GlobalPosition;
    }

    private void UpdateCamera()
    {
        Transform = _initialTransform;
        SetAsTopLevel(true);
    }
}