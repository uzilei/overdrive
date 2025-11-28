// dopo, se c'è tempo, dividiamo il codice in più file, perché è già un partial class
// la maggior parte della matematica e fisica è stata fatta con copilot perché non li ho ancora imparati mi dispiac

using Godot;
using System;

public partial class CarController : RigidBody3D
{
	[ExportGroup("Public")]
    [Export] public int PlayerId = 0;
    [Export] public Vector3 SpawnPosition = new();
    [Export] public Vector3 SpawnRotation = new();
	[Export] public int Team = 0;

	[ExportGroup("Per Car")]
	[Export] private float _acceleration = 8.3f;
	[Export] private float _maxSpeed = 75.0f;
	[Export] private float _airborneMaxSpeed = 112.5f;
	[Export] private float _rotationSpeed = 1.05f;
	[Export] private float _minTurnSpeed = 0.57f;
	[Export] private float _driftMaxSpeedBonus = 0.19f;
	[Export] private float _nitroConsumptionMultiplier = 1.0f;

	[ExportGroup("Preference")]
	[Export] private float _steeringSensitivity = 5.0f;
	[Export] private bool _autoAccelerate = true;
	[Export] private bool _debugPrintStats = false;

	[ExportGroup("Physics")]
	[Export] private float _downforce = 1.0f;
	[Export] private float _steerDeadzone = 0.75f;
	[Export] private float _driftStopSteerThreshold = 0.2f;
	[Export] private float _driftStopSteerDuration = 0.666f;
	[Export] private float _driftModelYawFactor = 1.5f;
	[Export] private float _yawSmooth = 8.0f;
	[Export] private float _autoRightSpeed = 3.0f;
	[Export] private float _autoRightNoSurfaceFactor = 1.5f;
	[Export] private float _airborneYawAlignSpeed = 4.0f;
	[Export] private float _spinDuration = 1.333f;
	[Export] private float _airSpinDuration = 1.0f;

	[ExportGroup("Integration")]
	[Export] public TimerLabel TimerUI;
	[Export] public PositionDisplay PositionUI;
	[Export] public RaceProgress RaceProgressUI;
	[Export] public NotificationLabel NotificationUI;

	[Export] public int CrimLives = 2;

	private const float MaxSpeedOverageThreshold = 1.0f;
	private const float WheelSpinFactor = 2.0f;
	private const float WheelBaseYawFactor = 0.5f;
	private const float BodyEmissionPressed = 1.0f;
	private const float BodyEmissionDefault = 10.0f;
	private const float PoliceFlashInterval = 0.2f;
	private const float MinTurnScale = 0.25f;
	private const float BrakeActionWindow = 0.333f;
	private const int MaxNitro = 100;

	private RayCast3D _rayFront;
	private RayCast3D _rayRear;
	private RayCast3D _rayFrontLeft;
	private RayCast3D _rayFrontRight;
	private RayCast3D _rayRearLeft;
	private RayCast3D _rayRearRight;
    private RayCast3D _rayTop;

	private MeshInstance3D _bodyMesh;
	private StandardMaterial3D _bodyMaterial;
	private GameManager _gameManager;
	private OmniLight3D _policeLight;

	private MeshInstance3D _wheelFrontLeft;
	private MeshInstance3D _wheelFrontRight;
	private MeshInstance3D _wheelRearLeft;
	private MeshInstance3D _wheelRearRight;
	private Node3D _wheelFrontLeftBase;
	private Node3D _wheelFrontRightBase;

	private CollisionShape3D _collisionCylinderFront;
	private CollisionShape3D _collisionCylinderRear;
	private CollisionShape3D _collisionCapsuleLeft;
	private CollisionShape3D _collisionCapsuleRight;
	private CollisionShape3D _collisionBox;
	private Transform3D _collisionCylinderFrontRel;
	private Transform3D _collisionCylinderRearRel;
	private Transform3D _collisionCapsuleLeftRel;
	private Transform3D _collisionCapsuleRightRel;
	private Transform3D _collisionBoxRel;

	private Node3D _modelNode;
	private Vector3 _modelInitialRotation;
	private PhysicsMaterial _physicsMaterialInstance;

	private enum NitroPhase { None, Yellow, Blue, Orange, Purple }
	private NitroPhase _nitroPhase = NitroPhase.None;
	private float _nitroAmount = 25f;
	private bool _awaitingNitroSecondPress = false;
	private float _nitroSecondPressTimer = 0f;
	private bool _nitroWasFull = false;

	private bool _isDrifting = false;
	private bool _isSpinning = false;
	private bool _isNitroBoosting = false;
    private bool _isExploding = false;
	private float _policeFlashTimer = 0f;
	private bool _policeFlashOn = false;
	private float _prevForwardSpeed = 0f;

	private float _spinElapsed = 0f;
	private float _spinTotalAngle = 0f;
	private float _currentSpinYaw = 0f;
	private float _spinAngleAccum = 0f;
	private float _prevSpinYawForAccum = 0f;
	private enum SpinPhase { None, Entering, Air, Exiting }
	private SpinPhase _spinPhase = SpinPhase.None;
	private float _spinPhaseDuration = 0f;
	private float _spinPhaseStartYaw = 0f;
	private float _spinPhaseDelta = 0f;
	private float _spinDir = 1f;

	private bool _brakeHeld = false;
	private float _brakePressTimer = 0f;
	private bool _awaitingSpinWindow = false;
	private float _spinWindowTimer = 0f;
	private float _steerBelowTimer = 0f;

	private float _modelYawVisual = 0f;
	private float _wheelYawVisual = 0f;

	private float _turnScaleCurrent = 1.0f;

	private float _steerSmoothed = 0.0f;
	private bool _inputForwardPressed = false;
	private bool _inputBrakePressed = false;

	private bool _previousGrounded = true;
	private float _turnSpeedOverride = float.NaN;

	private bool _prevDrifting = false;
	private bool _prevSpinGrounded = false;
	private NitroPhase _prevNitroPhase = NitroPhase.None;

	private Vector3 _LocalForward => -GlobalTransform.Basis.Z;
	private Vector3 _LocalForwardNormalized => (_LocalForward.LengthSquared() > 1e-6f) ? _LocalForward.Normalized() : Vector3.Zero;
	private Vector3 _LocalUp => GlobalTransform.Basis.Y;
	private Vector3 _LocalUpNormalized => (_LocalUp.LengthSquared() > 1e-6f) ? _LocalUp.Normalized() : Vector3.Up;

    public Vector3 RespawnPosition;
    public Vector3 RespawnRotation;

	public int CurrentCheckpoint = 0;
	public int RacePosition = 1;
	public bool RaceComplete = false;

	public bool Braking => _inputBrakePressed;
	public bool Drifting => _isDrifting;
	public int NitroPhaseLevel => (int)_nitroPhase;
	public bool NitroActive => _isNitroBoosting;
	public float NitroRemaining => _nitroAmount;
	public bool Exploding => _isExploding;
	public bool PurpleEligible => _nitroWasFull || (_nitroAmount >= (MaxNitro - 0.001f));

	[Signal] public delegate void DriftStartedEventHandler();
	[Signal] public delegate void DriftStoppedEventHandler();
	[Signal] public delegate void SpinGroundedStartedEventHandler();
	[Signal] public delegate void SpinGroundedStoppedEventHandler();
	[Signal] public delegate void NitroYellowOnEventHandler();
	[Signal] public delegate void NitroYellowOffEventHandler();
	[Signal] public delegate void NitroBlueOnEventHandler();
	[Signal] public delegate void NitroBlueOffEventHandler();
	[Signal] public delegate void NitroOrangeOnEventHandler();
	[Signal] public delegate void NitroOrangeOffEventHandler();
	[Signal] public delegate void NitroPurpleOnEventHandler();
	[Signal] public delegate void NitroPurpleOffEventHandler();
	[Signal] public delegate void BlueBottlePickupEventHandler();
	[Signal] public delegate void OrangeBottlePickupEventHandler();
	[Signal] public delegate void ExplosionEventHandler();

	public override void _Ready()
	{
        RespawnPosition = SpawnPosition;
        RespawnRotation = SpawnRotation;

        GlobalPosition = RespawnPosition;
        GlobalRotationDegrees = RespawnRotation;

		_rayFront = GetNode<RayCast3D>("RayCast3DF");
		_rayRear = GetNode<RayCast3D>("RayCast3DR");
		_rayFrontLeft = GetNode<RayCast3D>("RayCast3DFL");
		_rayFrontRight = GetNode<RayCast3D>("RayCast3DFR");
		_rayRearLeft = GetNode<RayCast3D>("RayCast3DRL");
		_rayRearRight = GetNode<RayCast3D>("RayCast3DRR");
        _rayTop = GetNode<RayCast3D>("RayCast3DTop");

		RayCast3D[] allRays = { _rayFront, _rayRear, _rayFrontLeft, _rayFrontRight, _rayRearLeft, _rayRearRight };
		foreach (var r in allRays)
			r.Enabled = true;

		_modelNode = GetNode<Node3D>("Model");
		var model = _modelNode;

		_bodyMesh = model.GetNode<MeshInstance3D>("Body");
		Material mat = _bodyMesh.Mesh.SurfaceGetMaterial(0);
		if (mat is StandardMaterial3D smd)
		{
			var inst = (StandardMaterial3D)smd.Duplicate(true);
			_bodyMesh.SetSurfaceOverrideMaterial(0, inst);
			_bodyMaterial = inst;
			_bodyMaterial.EmissionEnabled = true;
			_bodyMaterial.EmissionEnergyMultiplier = BodyEmissionDefault;
		}
		_policeLight = model.GetNodeOrNull<OmniLight3D>("PoliceLight");
		_modelInitialRotation = _modelNode.Rotation;

		_wheelFrontLeftBase = model.GetNode<Node3D>("WheelFlBase");
		_wheelFrontRightBase = model.GetNode<Node3D>("WheelFrBase");
		_wheelFrontLeft = _wheelFrontLeftBase.GetNode<MeshInstance3D>("WheelFl");
		_wheelFrontRight = _wheelFrontRightBase.GetNode<MeshInstance3D>("WheelFr");
		_wheelRearLeft = model.GetNode<MeshInstance3D>("WheelRl");
		_wheelRearRight = model.GetNode<MeshInstance3D>("WheelRr");

		_collisionCylinderFront = GetNode<CollisionShape3D>("CollisionShape3DCylinderF");
		_collisionCylinderRear = GetNode<CollisionShape3D>("CollisionShape3DCylinderR");
		_collisionCapsuleLeft = GetNode<CollisionShape3D>("CollisionShape3DCapsuleL");
		_collisionCapsuleRight = GetNode<CollisionShape3D>("CollisionShape3DCapsuleR");
		_collisionBox = GetNode<CollisionShape3D>("CollisionShape3DBox");

		var modelGlobal = _modelNode.GlobalTransform;
		var invModel = modelGlobal.AffineInverse();
		_collisionCylinderFrontRel = invModel * _collisionCylinderFront.GlobalTransform;
		_collisionCylinderRearRel = invModel * _collisionCylinderRear.GlobalTransform;
		_collisionCapsuleLeftRel = invModel * _collisionCapsuleLeft.GlobalTransform;
		_collisionCapsuleRightRel = invModel * _collisionCapsuleRight.GlobalTransform;
		_collisionBoxRel = invModel * _collisionBox.GlobalTransform;

		var pmDup = (PhysicsMaterial)PhysicsMaterialOverride.Duplicate(true);
		PhysicsMaterialOverride = pmDup;
		_physicsMaterialInstance = pmDup;
		_previousGrounded = IsGrounded();
		_physicsMaterialInstance.Friction = _previousGrounded ? 1.0f : 0.5f;
		_turnScaleCurrent = 1.0f;

		_prevDrifting = _isDrifting;
		_prevSpinGrounded = _isSpinning && IsGrounded();
		_prevNitroPhase = _nitroPhase;

		RegisterWithGameManager();
	}

	private void RegisterWithGameManager()
	{
		Node parent = GetParent();
		while (parent != null)
		{
			if (parent is GameManager gm)
			{
				_gameManager = gm;
				if (gm.HasMethod("RegisterCar"))
				{
					gm.Call("RegisterCar", this);
				}
				break;
			}
			parent = parent.GetParent();
		}
	}

	private void _UpdateInputState(double delta)
	{
		if (Input.IsActionPressed("debugexplode")) Explode();

		float analog = 0f;
		if (InputMap.HasAction($"left{PlayerId}") || InputMap.HasAction($"right{PlayerId}"))
			analog = Input.GetActionStrength($"right{PlayerId}") - Input.GetActionStrength($"left{PlayerId}");
		float kbd = 0f;
		if (Input.IsActionPressed("kright"))
			kbd += 1f;
		if (Input.IsActionPressed("kleft"))
			kbd -= 1f;
		float targetSteer = Mathf.Clamp(analog + kbd, -1f, 1f);

		_steerSmoothed = Mathf.Lerp(_steerSmoothed, targetSteer, Mathf.Min(1f, (float)delta * _steeringSensitivity));

		_inputForwardPressed = Input.IsActionPressed($"forward{PlayerId}") || _autoAccelerate;
		bool currentBrake = Input.IsActionPressed($"backward{PlayerId}");
		_inputBrakePressed = currentBrake;


		if (currentBrake)
		{
			if (!_brakeHeld)
			{
				if (_awaitingSpinWindow && _spinWindowTimer <= BrakeActionWindow)
				{
					StartSpin();
				}
				else
				{
					if (!_isDrifting)
						StartDrift();
					else
						StopDrift();
				}

				_awaitingSpinWindow = true;
				_spinWindowTimer = 0f;

				if (_isNitroBoosting)
				{
					_isNitroBoosting = false;
					_nitroPhase = NitroPhase.None;
					_awaitingNitroSecondPress = false;
					_nitroWasFull = false;
				}
				_brakeHeld = true;
				_brakePressTimer = 0f;
			}
			else
			{
				_brakePressTimer += (float)delta;
				if (_isDrifting && _brakePressTimer >= BrakeActionWindow)
				{
					StopDrift();
				}
			}
		}
		else
		{
			_brakeHeld = false;
			_brakePressTimer = 0f;
		}

		if (Input.IsActionJustPressed($"nitro{PlayerId}"))
		{
			if (_nitroPhase == NitroPhase.None && _nitroAmount > 0f)
			{
				_nitroWasFull = _nitroAmount >= (MaxNitro - 0.001f);
				_nitroPhase = NitroPhase.Yellow;
				_isNitroBoosting = true;
				_awaitingNitroSecondPress = true;
				_nitroSecondPressTimer = 0f;
				if (_isDrifting)
					StopDrift();
			}
			else if (_nitroPhase == NitroPhase.Yellow)
			{
				float secondPressTime = _nitroSecondPressTimer;
				if (secondPressTime >= 0.6f && secondPressTime <= 1.8f)
				{
					_nitroPhase = NitroPhase.Blue;
					_isNitroBoosting = true;
					_awaitingNitroSecondPress = false;
				}
				else if (secondPressTime < 0.6f)
				{
					if (_nitroWasFull)
						_nitroPhase = NitroPhase.Purple;
					else
						_nitroPhase = NitroPhase.Orange;
					_isNitroBoosting = true;
					_awaitingNitroSecondPress = false;
					_nitroWasFull = false;
				}
				else
				{
					_nitroPhase = NitroPhase.Orange;
					_isNitroBoosting = true;
					_awaitingNitroSecondPress = false;
					_nitroWasFull = false;
				}
			}
		}

		if (_bodyMaterial != null)
		{
			if (Team == 1)
			{
				_policeFlashTimer += (float)delta;
				if (_policeFlashTimer >= PoliceFlashInterval)
				{
					_policeFlashTimer -= PoliceFlashInterval;
					_policeFlashOn = !_policeFlashOn;
				}
				_bodyMaterial.EmissionEnergyMultiplier = _policeFlashOn ? BodyEmissionDefault : BodyEmissionPressed;
				if (_policeLight != null)
					_policeLight.Visible = _policeFlashOn;
			}
			else
			{
				_bodyMaterial.EmissionEnergyMultiplier = (currentBrake || _isDrifting) ? BodyEmissionDefault : BodyEmissionPressed;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		ResetRayRotations();

		Vector3 forward = _LocalForwardNormalized;
		if (forward == Vector3.Zero)
			return;

		float forwardSpeed = LinearVelocity.Dot(forward);
		float forwardSpeedAbs = MathF.Abs(forwardSpeed);
		if (_prevForwardSpeed > 25f && forwardSpeedAbs < 0.5f * _prevForwardSpeed)
		{
			Explode();
		}

		float spinAmount = forwardSpeed * WheelSpinFactor * (float)delta;
		RotateWheels(spinAmount);

		float angVelYaw = AngularVelocity.Y;
		if (_awaitingSpinWindow)
		{
			_spinWindowTimer += (float)delta;
			if (_spinWindowTimer > BrakeActionWindow)
				_awaitingSpinWindow = false;
		}
		ApplyDriftVisuals((float)delta, angVelYaw);
		_prevForwardSpeed = forwardSpeedAbs;

		if (_awaitingNitroSecondPress)
		{
			_nitroSecondPressTimer += (float)delta;
			if (_nitroSecondPressTimer > 2.0f)
			{
				_awaitingNitroSecondPress = false;
				_nitroWasFull = false;
			}
		}

		float deltaF = (float)delta;
		if (_isDrifting && IsGrounded())
			_nitroAmount += 20f * deltaF;
		if (!_isNitroBoosting && !IsGrounded() && !Freeze)
			_nitroAmount += 10f * deltaF;

		if (!_isNitroBoosting && !Freeze)
		{
			switch (RacePosition)
			{
				case 2:
					_nitroAmount += 5f * deltaF;
					break;
				case 3:
					_nitroAmount += 7.5f * deltaF;
					break;
				case 4:
					_nitroAmount += 10f * deltaF;
					break;
			}
		}

		if (_isNitroBoosting)
		{
			float consumeRate = 0f;
			switch (_nitroPhase)
			{
				case NitroPhase.Yellow: consumeRate = 15f; break;
				case NitroPhase.Blue: consumeRate = 25f; break;
				case NitroPhase.Orange: consumeRate = 35f; break;
				case NitroPhase.Purple: consumeRate = 50f; break;
			}
			consumeRate *= _nitroConsumptionMultiplier;
			if (!IsGrounded())
				consumeRate *= 0.5f;
			_nitroAmount -= consumeRate * deltaF;
			if (_nitroAmount <= 0f)
			{
				_nitroAmount = 0f;
				_isNitroBoosting = false;
				_nitroPhase = NitroPhase.None;
				_awaitingNitroSecondPress = false;
				_nitroWasFull = false;
			}
		}
		if (_nitroAmount > MaxNitro) _nitroAmount = MaxNitro;

		float speedRatio = 0.0f;
		if (_maxSpeed > 0.0001f)
			speedRatio = Mathf.Min(Mathf.Abs(forwardSpeed) / _maxSpeed, 1.0f);
		float turnScale = 1.0f - (1.0f - MinTurnScale) * speedRatio;

		_UpdateInputState(delta);
		float rawAbsSteer = Mathf.Min(1.0f, Mathf.Abs(_steerSmoothed));
		float absSteer = 0f;
		if (_isDrifting)
		{
			absSteer = rawAbsSteer;
		}
		else
		{
			if (rawAbsSteer > _steerDeadzone)
			{
				absSteer = (rawAbsSteer - _steerDeadzone) / (1f - _steerDeadzone);
			}
		}

		float extraNitroAccel;

		GetNitroModifiers(out _, out extraNitroAccel, out _, out _);

		bool isGrounded = IsGrounded();
		if (isGrounded != _previousGrounded)
		{
			_physicsMaterialInstance.Friction = isGrounded ? 0f : 0.5f;
            GravityScale = isGrounded ? 1f : 2f;
			_previousGrounded = isGrounded;
		}

		if (!isGrounded && _isNitroBoosting && extraNitroAccel > 0f)
			ApplyCentralForce(forward * extraNitroAccel);

		if (_isDrifting)
		{
			if (rawAbsSteer < _driftStopSteerThreshold)
			{
				_steerBelowTimer += (float)delta;
				if (_steerBelowTimer >= _driftStopSteerDuration)
					StopDrift();
			}
			else
			{
				_steerBelowTimer = 0f;
			}
		}

		float speedTotal = LinearVelocity.Length();

		if (_isDrifting && speedTotal < _maxSpeed * 0.1f)
			StopDrift();

		float targetTurnScale = turnScale;
		float effectiveMaxSpeed = CalculateMaxSpeed(isGrounded, absSteer, _isDrifting);
		if (_isDrifting)
		{
			targetTurnScale = 1.0f;
		}
		else if (_isSpinning)
		{
			targetTurnScale = MinTurnScale;
		}

		if (_debugPrintStats)
		{
			GD.Print($"Speed={speedTotal:F2} forward={forwardSpeed:F2} grounded={isGrounded} nitro={_nitroPhase}:{_nitroAmount}");
		}

		float smoothT = Mathf.Min(1f, _yawSmooth * 0.33f * (float)delta);
		_turnScaleCurrent = _turnScaleCurrent + (targetTurnScale - _turnScaleCurrent) * smoothT;

		float effectiveTurnSpeed = CalculateTurnSpeed(_turnScaleCurrent);
		_turnSpeedOverride = effectiveTurnSpeed;



		Drive(forward, forwardSpeed, effectiveMaxSpeed, isGrounded, speedTotal);
		EmitStateSignals();
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		Vector3 forward = _LocalForwardNormalized;
		if (forward == Vector3.Zero)
			return;

		int contactCount;
		try { contactCount = state.GetContactCount(); } catch { contactCount = 0; }
		for (int i = 0; i < contactCount; i++)
		{
			var collider = state.GetContactColliderObject(i);
			if (collider is CarController other && other != this)
			{
				int myDurability = GetDurability();
				int otherDurability = other.GetDurability();
				if (myDurability < otherDurability)
				{
					if (!_isExploding) Explode();
				}
				else if (myDurability > otherDurability)
				{
					if (Team == 1 && other.Team == 0)
					{
						other.CrimLives--;
						if (other.CrimLives <= 0)
						{
							if (other.NotificationUI != null)
								other.NotificationUI.ShowLapsRemaining(0);
							other.RaceComplete = true;
						}
					}
					if (!other.Exploding) other.Explode();
				}
			}
		}

        if (_rayTop.IsColliding()) Explode();

		bool isGrounded = IsGrounded();

		if (isGrounded)
		{

			float speedAlongForward = state.LinearVelocity.Dot(forward);
			Vector3 forwardOnly = forward * speedAlongForward;
			state.LinearVelocity = new Vector3(forwardOnly.X, state.LinearVelocity.Y, forwardOnly.Z);

			float speedTotal = state.LinearVelocity.Length();
			if (speedTotal > 0.0001f && _maxSpeed > 0.0001f)
			{

				float rawAbsSteer = Mathf.Min(1.0f, Mathf.Abs(_steerSmoothed));
				float absSteer = 0f;
				if (_isDrifting)
				{
					absSteer = rawAbsSteer;
				}
				else
				{
					if (rawAbsSteer > _steerDeadzone)
						absSteer = (rawAbsSteer - _steerDeadzone) / (1f - _steerDeadzone);
				}
				float effectiveMaxSpeed = CalculateMaxSpeed(true, absSteer, _isDrifting);
				if (speedTotal > effectiveMaxSpeed + MaxSpeedOverageThreshold)
				{
					float stepDt = 0.016f;
					try { stepDt = (float)state.Step; } catch { }
					Vector3 linVel = state.LinearVelocity;
					Vector3 reduced = linVel - linVel.Normalized() * _acceleration * stepDt;
					if (reduced.Length() < 0f)
						reduced = Vector3.Zero;
					state.LinearVelocity = new Vector3(reduced.X, reduced.Y, reduced.Z);
				}
			}
		}
		else
		{
			float speedTotal = state.LinearVelocity.Length();
			if (speedTotal > 0.0001f && _airborneMaxSpeed > 0.0001f && speedTotal > _airborneMaxSpeed)
			{
				Vector3 dir = state.LinearVelocity.Normalized();
				state.LinearVelocity = dir * _airborneMaxSpeed;
			}
		}


		if (!isGrounded)
		{
			Vector3 sumNormal = Vector3.Zero;
			int n = 0;
			RayCast3D[] cornerRays = { _rayFrontLeft, _rayFrontRight, _rayRearLeft, _rayRearRight };
			foreach (var r in cornerRays)
			{
				if (r != null && r.IsColliding())
				{
					sumNormal += r.GetCollisionNormal();
					n++;
				}
			}

			Vector3 targetUp;
			float speedFactor = _autoRightSpeed;
			if (n > 0)
			{
				targetUp = (sumNormal / n).Normalized();
			}
			else
			{
				targetUp = Vector3.Up;
				speedFactor *= _autoRightNoSurfaceFactor;
			}

			Vector3 currentUp = GlobalTransform.Basis.Y.Normalized();
			float dot = Mathf.Clamp(currentUp.Dot(targetUp), -1f, 1f);
			float angle = Mathf.Acos(dot);
			if (angle > 0.001f)
			{
				Vector3 axis = currentUp.Cross(targetUp);
				if (axis.LengthSquared() > 1e-6f)
					axis = axis.Normalized();
				else
					axis = Vector3.Zero;

				Vector3 desiredAngVel = axis * (angle * speedFactor);

				desiredAngVel = new Vector3(desiredAngVel.X, 0f, desiredAngVel.Z);

				float stepDt = 0.016f;
				try { stepDt = (float)state.Step; } catch { }

				Vector3 currentAv = state.AngularVelocity;
				float blendT = Mathf.Min(1f, speedFactor * stepDt);
				Vector3 blended = currentAv + (desiredAngVel - new Vector3(currentAv.X, 0f, currentAv.Z)) * blendT;
				blended.Y = currentAv.Y;
				state.AngularVelocity = blended;
			}
		}


		float forwardSpeed = state.LinearVelocity.Dot(forward);
		float speedRatio = 0.0f;
		if (_maxSpeed > 0.0001f)
			speedRatio = Mathf.Min(Mathf.Abs(forwardSpeed) / _maxSpeed, 1.0f);
		float turnScale = 1.0f - (1.0f - MinTurnScale) * speedRatio;
		float effectiveTurnSpeed = _rotationSpeed * turnScale;

		if (!float.IsNaN(_turnSpeedOverride))
		{
			effectiveTurnSpeed = _turnSpeedOverride;
			_turnSpeedOverride = float.NaN;
		}

		Vector3 angVel = state.AngularVelocity;
		float speed = state.LinearVelocity.Length();


		float airborneYawVel = 0f;
		if (!isGrounded)
		{
			Vector3 velHoriz = new Vector3(state.LinearVelocity.X, 0f, state.LinearVelocity.Z);
			float velLen = velHoriz.Length();
			if (velLen > 0.001f)
			{
				Vector3 desiredDir = velHoriz / velLen;
				Vector3 forwardHoriz = new Vector3(forward.X, 0f, forward.Z);
				float fLen = forwardHoriz.Length();
				if (fLen > 0.001f)
					forwardHoriz = forwardHoriz / fLen;
				else
					forwardHoriz = desiredDir;

				float dot = Mathf.Clamp(forwardHoriz.Dot(desiredDir), -1f, 1f);
				float angle = Mathf.Acos(dot);
				float crossY = forwardHoriz.Cross(desiredDir).Y;
				float sign = Mathf.Sign(crossY);
				airborneYawVel = sign * angle * _airborneYawAlignSpeed;
			}

			float yawInputRate = -_steerSmoothed * (_rotationSpeed * 0.2f);
			float stepDt = 0.016f;
			try { stepDt = (float)state.Step; } catch { }
			float yawAngle = yawInputRate * stepDt;
			if (Mathf.Abs(yawAngle) > 1e-6f)
			{
				var linVel = state.LinearVelocity;
				Vector3 linVelHoriz = new Vector3(linVel.X, 0f, linVel.Z);
				Vector3 rotated = linVelHoriz.Rotated(Vector3.Up, yawAngle);
				state.LinearVelocity = new Vector3(rotated.X, linVel.Y, rotated.Z);
			}
		}

		if (isGrounded && speed >= 0.1f)
			angVel.Y = -_steerSmoothed * effectiveTurnSpeed;
		else
		{
			angVel.Y = airborneYawVel;
		}
		state.AngularVelocity = angVel;
	}

	public void OnNitroBottleEntered(int nitroToAdd)
	{
		_nitroAmount += nitroToAdd;
		if (nitroToAdd == 25) EmitSignal(SignalName.BlueBottlePickup);
		else if (nitroToAdd == 50) EmitSignal(SignalName.OrangeBottlePickup);
	}

	private void GetNitroModifiers(out float speedMultiplier, out float extraAccel, out float turnRateMultiplier, out bool disableTurningDecel)
	{
		speedMultiplier = 1.0f;
		extraAccel = 0f;
		turnRateMultiplier = 1.0f;
		disableTurningDecel = false;
		if (!_isNitroBoosting)
			return;
		switch (_nitroPhase)
		{
			case NitroPhase.Yellow:
				speedMultiplier = 1.05f;
				extraAccel = _acceleration * 0.5f;
				disableTurningDecel = false;
				break;
			case NitroPhase.Blue:
				speedMultiplier = 1.075f;
				extraAccel = _acceleration * 0.75f;
				turnRateMultiplier = 1.5f;
				disableTurningDecel = true;
				break;
			case NitroPhase.Orange:
				speedMultiplier = 1.05f;
				extraAccel = _acceleration * 1.0f;
				turnRateMultiplier = 0.9f;
				disableTurningDecel = true;
				break;
			case NitroPhase.Purple:
				speedMultiplier = 1.05f;
				extraAccel = _acceleration * 2.0f;
				turnRateMultiplier = 0.75f;
				disableTurningDecel = true;
				break;
		}
	}

	private float CalculateMaxSpeed(bool isGrounded, float absSteer, bool _isDrifting = false)
	{
		float baseMax;
		if (_isDrifting)
			baseMax = _maxSpeed * (1.0f - (1.0f - _minTurnSpeed - _driftMaxSpeedBonus) * absSteer);
		else
			baseMax = _maxSpeed * (1.0f - (1.0f - _minTurnSpeed) * absSteer);
		if (isGrounded)
		{
			GetNitroModifiers(out float speedMultiplier, out _, out _, out _);
			if (_isNitroBoosting && _nitroPhase != NitroPhase.Yellow)
			{
				baseMax = _maxSpeed * speedMultiplier;
			}
			else
			{
				baseMax *= speedMultiplier;
			}
		}

		return baseMax;
	}

	private float CalculateTurnSpeed(float turnScale)
	{
		GetNitroModifiers(out _, out _, out float turnRateMultiplier, out _);
		return _rotationSpeed * turnScale * turnRateMultiplier;
	}

	private float CalculateAcceleration(float speedTotal)
	{
		const float MinFactor = 0.25f;
		float baseAccel = _acceleration * ((1 - Mathf.Pow(speedTotal / _maxSpeed, 2)) * (1 - MinFactor) + MinFactor);
		if (_isSpinning || _isDrifting)
			baseAccel = 0.2f;

		GetNitroModifiers(out _, out float extraAccel, out _, out _);
		return baseAccel + extraAccel;
	}

	private void Drive(Vector3 forward, float forwardSpeed, float effectiveMaxSpeed, bool isGrounded, float speedTotal)
	{
		if (!isGrounded)
			return;

		bool rawForward = _inputForwardPressed;
		bool rawBackward = _inputBrakePressed;
		bool turning = Mathf.Abs(_steerSmoothed) > 0f;
		bool turningDecelEnabled = true;
		GetNitroModifiers(out _, out _, out _, out bool disableTurningDecel);
		if (disableTurningDecel)
			turningDecelEnabled = false;
		bool forwardPressed = rawForward;
		bool backwardPressed = rawBackward;
		if (turning && turningDecelEnabled)
		{
			if (rawForward && forwardSpeed >= effectiveMaxSpeed)
				forwardPressed = false;
			if (rawBackward && -forwardSpeed >= effectiveMaxSpeed)
				backwardPressed = false;
		}
		if (forwardPressed && backwardPressed)
			forwardPressed = false;

		float accel = CalculateAcceleration(speedTotal);
		if (forwardPressed && forwardSpeed < effectiveMaxSpeed)
			ApplyCentralForce(forward * accel);

		if (backwardPressed && forwardSpeed > -effectiveMaxSpeed)
			ApplyCentralForce(-forward * accel);

		if (_downforce > 0.0f)
		{
			float downMagnitude = Mathf.Abs(forwardSpeed) * _downforce;
			Vector3 downDir = -_LocalUpNormalized;
			ApplyCentralForce(downDir * downMagnitude);
		}

		if (!forwardPressed && !backwardPressed)
		{
			Vector3 velXZ = new(LinearVelocity.X, 0, LinearVelocity.Z);
			float speedXZ = velXZ.Length();
			if (speedXZ > 0.001f)
			{
				Vector3 decelDir = -velXZ.Normalized();
				ApplyCentralForce(decelDir * (_acceleration * 0.75f));
			}
		}
	}

	private void ApplyDriftVisuals(float delta, float angVelYaw)
	{
		float effectiveDriftYawFactor = IsGrounded() ? _driftModelYawFactor : _driftModelYawFactor * 4f;
		float baseYaw = angVelYaw * WheelBaseYawFactor;
		float targetWheelYaw = _isDrifting ? -baseYaw * effectiveDriftYawFactor : baseYaw;
		float wheelLerp = Mathf.Min(1f, _yawSmooth * delta);
		_wheelYawVisual += (targetWheelYaw - _wheelYawVisual) * wheelLerp;
		var rFl = _wheelFrontLeftBase.Rotation;
		rFl.Y = _wheelYawVisual;
		_wheelFrontLeftBase.Rotation = rFl;
		var rFr = _wheelFrontRightBase.Rotation;
		rFr.Y = _wheelYawVisual;
		_wheelFrontRightBase.Rotation = rFr;

		float targetModelYaw = _isDrifting ? baseYaw * effectiveDriftYawFactor : 0f;
		float modelLerp = Mathf.Min(1f, _yawSmooth * delta);
		_modelYawVisual = _modelYawVisual + (targetModelYaw - _modelYawVisual) * modelLerp;

		UpdateSpinPhase(delta);

		float combinedYaw = _modelYawVisual + _currentSpinYaw;
		_modelNode.Rotation = new Vector3(_modelInitialRotation.X, _modelInitialRotation.Y + combinedYaw, _modelInitialRotation.Z);

		AlignCollisionNodes();
	}

	private void UpdateSpinPhase(float delta)
	{
		if (!_isSpinning)
			return;

		switch (_spinPhase)
		{
			case SpinPhase.Entering:
				_spinElapsed += delta;
				{
					float phaseProgress = Mathf.Min(1f, _spinElapsed / _spinPhaseDuration);
					float easedIn = 1f - Mathf.Cos(MathF.PI * 0.5f * phaseProgress);
					_currentSpinYaw = _spinPhaseStartYaw + _spinPhaseDelta * easedIn;
					float deltaYawEnter = _currentSpinYaw - _prevSpinYawForAccum;
					_spinAngleAccum += Mathf.Abs(deltaYawEnter);
					_prevSpinYawForAccum = _currentSpinYaw;
					float fullEnter = MathF.PI * 2f;
					if (_spinAngleAccum >= fullEnter)
					{
						int spinsEnter = (int)(_spinAngleAccum / fullEnter);
						for (int i = 0; i < spinsEnter; i++)
						{
							_nitroAmount += 50f;
							if (_nitroAmount > MaxNitro) { _nitroAmount = MaxNitro; break; }
						}
						_spinAngleAccum -= spinsEnter * fullEnter;
					}
					if (phaseProgress >= 1f)
					{
						bool grounded = IsGrounded();
						if (grounded)
						{
							_spinPhase = SpinPhase.Exiting;
							_spinPhaseDuration = _spinDuration * 0.5f;
							_spinPhaseStartYaw = _currentSpinYaw;
							_spinPhaseDelta = _spinDir * MathF.PI;
							_spinElapsed = 0f;
						}
						else
						{
							_spinPhase = SpinPhase.Air;
							_spinPhaseDuration = _airSpinDuration;
							_spinPhaseStartYaw = _currentSpinYaw;
							_spinPhaseDelta = _spinDir * MathF.PI * 2f;
							_spinElapsed = 0f;
						}
					}
				}
				break;
			case SpinPhase.Air:
				_spinElapsed += delta;
				{
					float phaseProgress = Mathf.Min(1f, _spinElapsed / _spinPhaseDuration);
					_currentSpinYaw = _spinPhaseStartYaw + _spinPhaseDelta * phaseProgress;
					float deltaYaw = _currentSpinYaw - _prevSpinYawForAccum;
					_spinAngleAccum += Mathf.Abs(deltaYaw);
					_prevSpinYawForAccum = _currentSpinYaw;
					float full = MathF.PI * 2f;
					if (_spinAngleAccum >= full)
					{
						int spins = (int)(_spinAngleAccum / full);
						for (int i = 0; i < spins; i++)
						{
							_nitroAmount += 50f;
							if (_nitroAmount > MaxNitro) { _nitroAmount = MaxNitro; break; }
						}
						_spinAngleAccum -= spins * full;
					}

					if (phaseProgress >= 1f)
					{
						bool grounded = IsGrounded();
						if (grounded)
						{
							_spinPhase = SpinPhase.Exiting;
							_spinPhaseDuration = _spinDuration * 0.5f;
							_spinPhaseStartYaw = _currentSpinYaw;
							_spinPhaseDelta = _spinDir * MathF.PI;
							_spinElapsed = 0f;
						}
						else
						{
							_spinPhase = SpinPhase.Air;
							_spinPhaseDuration = _airSpinDuration;
							_spinPhaseStartYaw = _currentSpinYaw;
							_spinPhaseDelta = _spinDir * MathF.PI * 2f;
							_spinElapsed = 0f;
						}
					}
				}
				break;
			case SpinPhase.Exiting:
				_spinElapsed += delta;
				{
					float phaseProgress = Mathf.Min(1f, _spinElapsed / _spinPhaseDuration);
					float easedOut = Mathf.Sin((MathF.PI * 0.5f) * phaseProgress);
					_currentSpinYaw = _spinPhaseStartYaw + _spinPhaseDelta * easedOut;
					float deltaYawExit = _currentSpinYaw - _prevSpinYawForAccum;
					_spinAngleAccum += Mathf.Abs(deltaYawExit);
					_prevSpinYawForAccum = _currentSpinYaw;
					float fullExit = MathF.PI * 2f;
					if (_spinAngleAccum >= fullExit)
					{
						int spinsExit = (int)(_spinAngleAccum / fullExit);
						for (int i = 0; i < spinsExit; i++)
						{
							_nitroAmount += 50f;
							if (_nitroAmount > MaxNitro) { _nitroAmount = MaxNitro; break; }
						}
						_spinAngleAccum -= spinsExit * fullExit;
					}
					if (phaseProgress >= 1f)
						StopSpin();
				}
				break;
		}
	}

	private void RotateWheels(float spinAmount)
	{
		_wheelFrontLeft.RotateX(-spinAmount);
		_wheelFrontRight.RotateX(-spinAmount);
		_wheelRearLeft.RotateX(-spinAmount);
		_wheelRearRight.RotateX(-spinAmount);
	}

	private void ResetRayRotations()
	{
		_rayFrontLeft.GlobalRotation = Vector3.Zero;
		_rayFrontRight.GlobalRotation = Vector3.Zero;
		_rayRearLeft.GlobalRotation = Vector3.Zero;
		_rayRearRight.GlobalRotation = Vector3.Zero;
	}

	private void EmitStateSignals()
	{
		bool curDrifting = _isDrifting && IsGrounded();
		if (curDrifting != _prevDrifting)
		{
			if (curDrifting)
				EmitSignal(SignalName.DriftStarted);
			else
				EmitSignal(SignalName.DriftStopped);
			_prevDrifting = curDrifting;
		}

		bool curSpinGrounded = _isSpinning && IsGrounded();
		if (curSpinGrounded != _prevSpinGrounded)
		{
			if (curSpinGrounded)
			{
				EmitSignal(SignalName.SpinGroundedStarted);
				_prevSpinGrounded = true;
			}
			else
			{
				if (!_isDrifting)
				{
					EmitSignal(SignalName.SpinGroundedStopped);
					_prevSpinGrounded = false;
				}
			}
		}

		bool curYellow = _nitroPhase == NitroPhase.Yellow;
		bool prevYellow = _prevNitroPhase == NitroPhase.Yellow;
		if (curYellow != prevYellow)
		{
			if (curYellow) EmitSignal(SignalName.NitroYellowOn); else EmitSignal(SignalName.NitroYellowOff);
		}
		bool curBlue = _nitroPhase == NitroPhase.Blue;
		bool prevBlue = _prevNitroPhase == NitroPhase.Blue;
		if (curBlue != prevBlue)
		{
			if (curBlue) EmitSignal(SignalName.NitroBlueOn); else EmitSignal(SignalName.NitroBlueOff);
		}
		bool curOrange = _nitroPhase == NitroPhase.Orange;
		bool prevOrange = _prevNitroPhase == NitroPhase.Orange;
		if (curOrange != prevOrange)
		{
			if (curOrange) EmitSignal(SignalName.NitroOrangeOn); else EmitSignal(SignalName.NitroOrangeOff);
		}
		bool curPurple = _nitroPhase == NitroPhase.Purple;
		bool prevPurple = _prevNitroPhase == NitroPhase.Purple;
		if (curPurple != prevPurple)
		{
			if (curPurple) EmitSignal(SignalName.NitroPurpleOn); else EmitSignal(SignalName.NitroPurpleOff);
		}

		_prevNitroPhase = _nitroPhase;
	}

	private void AlignCollisionNodes()
	{
		var modelGlobal = _modelNode.GlobalTransform;
		_collisionCylinderFront.GlobalTransform = modelGlobal * _collisionCylinderFrontRel;
		_collisionCylinderRear.GlobalTransform = modelGlobal * _collisionCylinderRearRel;
		_collisionCapsuleLeft.GlobalTransform = modelGlobal * _collisionCapsuleLeftRel;
		_collisionCapsuleRight.GlobalTransform = modelGlobal * _collisionCapsuleRightRel;
		_collisionBox.GlobalTransform = modelGlobal * _collisionBoxRel;
	}

	private void StartDrift()
	{

		if (_isNitroBoosting)
		{
			_isNitroBoosting = false;
			_nitroPhase = NitroPhase.None;
			_awaitingNitroSecondPress = false;
			_nitroWasFull = false;
		}
		_isDrifting = true;
		_steerBelowTimer = 0f;
		_awaitingSpinWindow = true;
		_spinWindowTimer = 0f;
	}

	private void StopDrift()
	{
		_isDrifting = false;
		_steerBelowTimer = 0f;
	}

	private void StartSpin()
	{
		bool isGroundedNow = IsGrounded();
		if (!isGroundedNow)
			return;

		if (_isSpinning)
			return;
		float av = AngularVelocity.Y;
		_spinDir = Mathf.Abs(av) > 0.1f ? Mathf.Sign(av) : (GD.Randf() < 0.5f ? -1f : 1f);
		_spinTotalAngle = _spinDir * MathF.PI * 2f;
		_spinPhase = SpinPhase.Entering;
		_spinPhaseDuration = _spinDuration * 0.5f;
		_spinPhaseStartYaw = _currentSpinYaw;
		_spinPhaseDelta = _spinDir * MathF.PI;
		_spinElapsed = 0f;
		_isSpinning = true;

		_spinAngleAccum = 0f;
		_prevSpinYawForAccum = _currentSpinYaw;
		if (_isDrifting)
			StopDrift();
		_awaitingSpinWindow = false;
		_spinWindowTimer = 0f;
	}

	private void StopSpin()
	{
		_isSpinning = false;
		_spinElapsed = 0f;
		_spinTotalAngle = 0f;
		_currentSpinYaw = 0f;

	}
    
	private bool IsGrounded()
	{
		return _rayFront.IsColliding() || _rayRear.IsColliding();
	}

	public int GetDurability()
	{
		if (_isNitroBoosting && _nitroPhase == NitroPhase.Purple)
			return 4;
		if (_isSpinning)
			return 3;
		if (_isNitroBoosting && _nitroPhase == NitroPhase.Blue)
			return 2;
		if (_isNitroBoosting && _nitroPhase == NitroPhase.Orange)
			return 1;
		return 0;
	}

	public async void Explode()
	{
		if (_isExploding)
			return;
		_isExploding = true;
        Freeze = true;
        _modelNode.Visible = false;
        _collisionBox.Disabled = true;
        _collisionCapsuleLeft.Disabled = true;
        _collisionCapsuleRight.Disabled = true;
        _collisionCylinderFront.Disabled = true;
        _collisionCylinderRear.Disabled = true;
		_isNitroBoosting = false;
		_nitroPhase = NitroPhase.None;
		_awaitingNitroSecondPress = false;
		_nitroWasFull = false;

		EmitSignal(SignalName.Explosion);
		
		if (RaceComplete)
			return;
		await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
        Freeze = false;
        _modelNode.Visible = true;
        _collisionBox.Disabled = false;
        _collisionCapsuleLeft.Disabled = false;
        _collisionCapsuleRight.Disabled = false;
        _collisionCylinderFront.Disabled = false;
        _collisionCylinderRear.Disabled = false;
        GlobalPosition = RespawnPosition;
        GlobalRotationDegrees = RespawnRotation;
		_isExploding = false;
        _nitroAmount = 100f;
	}
}