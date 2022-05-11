using Godot;
using SteveUtility;

public partial class PlayerController : RigidDynamicBody3D
{
	[Export] public float MaxRotationXDegrees = 90f;
	[Export] public Vector2 sensitivity = new Vector2(0.5f, 0.5f);
	[Export] public float accelerationMultiplier = 5f;
	[Export] public float deccelerationMultiplier = 1f;
	[Export] public float maxVelocity = 10f;
	[Export] public float stopVelocityTreshold = 1f;
	[Export] public int maxJumps = 2;
	[Export] public float wallrunTimeout = 2f;

	public bool IsSprinting { get; private set; } = false;
	public bool IsWallrunningRightSide { get; private set; } = false;
	public bool IsWallrunningLeftSide { get; private set; } = false;
	public Vector3 WallrunDirection { get; private set; } = Vector3.Zero;
	public Vector3 WallrunDirectionLastFrame { get; private set; } = Vector3.Zero;
	public Vector3 WallrunDirectionChange { get; private set; } = Vector3.Zero;
	public Vector3 WallNormal { get; private set; } = Vector3.Zero;
	public float TimeUntilNextWallrun { get; private set; } = 0f;
	public int jumpsLeft;

	public Camera3D camera;
	public CollisionShape3D collisionShape;

	private float defaultGravityScale;

	public override void _Ready()
	{
		base._Ready();

		BodyEntered += BodyEnteredCallback;

		camera = GetNode<Camera3D>("./CollisionShape/Camera");
		collisionShape = GetNode<CollisionShape3D>("./CollisionShape");
		defaultGravityScale = GravityScale;
		jumpsLeft = maxJumps;
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (Input.IsActionJustReleased("restart"))
		{
			GetTree().ReloadCurrentScene();

			LineDrawer lineDrawer = (LineDrawer)GetNode("/root/Debug").Get(nameof(DebugHelper.LineDrawer));
			lineDrawer.ClearLines();
		}

		if (Input.IsActionJustPressed("escape"))
		{
			if (Input.GetMouseMode() == Input.MouseMode.Visible)
			{
				Input.SetMouseMode(Input.MouseMode.Captured);
			}
			else
			{
				Input.SetMouseMode(Input.MouseMode.Visible);
			}
		}

		if (Input.IsActionJustPressed("jump"))
		{
			Jump();
		}

		Vector3 moveDirection = Vector3.Zero;
		IsSprinting = false;
		if (Input.IsActionPressed("sprint"))
		{
			IsSprinting = true;
		}

		if (!IsWallrunningRightSide && !IsWallrunningLeftSide)
		{
			if (Input.IsActionPressed("move_forward"))
			{
				moveDirection += -collisionShape.GlobalTransform.basis.z.Normalized() * 1000f * accelerationMultiplier * (IsSprinting ? 2f : 1f);
			}
			if (Input.IsActionPressed("move_backwards"))
			{
				moveDirection += collisionShape.GlobalTransform.basis.z.Normalized() * 1000f * accelerationMultiplier;
			}
			if (Input.IsActionPressed("move_right"))
			{
				moveDirection += collisionShape.GlobalTransform.basis.x.Normalized() * 1000f * accelerationMultiplier;
			}
			if (Input.IsActionPressed("move_left"))
			{
				moveDirection += -collisionShape.GlobalTransform.basis.x.Normalized() * 1000f * accelerationMultiplier;
			}
		}
		else if (IsWallrunningRightSide || IsWallrunningLeftSide)
		{
			if (Input.IsActionPressed("move_forward"))
			{
				moveDirection += -WallrunDirection * 4000f * accelerationMultiplier * (IsSprinting ? 2f : 1f);
			}
			if (Input.IsActionPressed("move_backwards"))
			{
				moveDirection += WallrunDirection * 4000f * accelerationMultiplier;
			}
			if (Input.IsActionPressed("move_right"))
			{
				moveDirection += WallrunDirection * 4000f * accelerationMultiplier;
			}
			if (Input.IsActionPressed("move_left"))
			{
				moveDirection += -WallrunDirection * 4000f * accelerationMultiplier;
			}
		}

		if (!Input.IsActionPressed("move_forward") && !Input.IsActionPressed("move_backwards"))
		{
			AddConstantCentralForce(-GetLinearVelocityLocal().z * collisionShape.GlobalTransform.basis.z * 1000f * deccelerationMultiplier);
		}
		if (!Input.IsActionPressed("move_right") && !Input.IsActionPressed("move_left"))
		{
			AddConstantCentralForce(-GetLinearVelocityLocal().x * collisionShape.GlobalTransform.basis.x * 1000f * deccelerationMultiplier);
		}

		AddConstantCentralForce(moveDirection);
	}

	public override void _PhysicsProcess(float delta)
	{
		base._PhysicsProcess(delta);

		foreach (RayCast3D rayCast in GetTree().GetNodesInGroup("wallrun_raycasts"))
		{
			if (!rayCast.Enabled || !rayCast.IsColliding())
			{
				continue;
			}

			Vector3 collisionNormal = rayCast.GetCollisionNormal();
			StartWallrun(false, collisionNormal);
			Wallrun(collisionNormal);
		}

		if (!IsWallrunningLeftSide && !IsWallrunningRightSide && TimeUntilNextWallrun > 0f)
		{
			TimeUntilNextWallrun -= delta;
		}
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		base._IntegrateForces(state);

		if (LinearVelocity.Abs().x > maxVelocity || LinearVelocity.Abs().z > maxVelocity)
		{
			LinearVelocity = new Vector3(LinearVelocity.Normalized().x * maxVelocity, LinearVelocity.y, LinearVelocity.Normalized().z * maxVelocity);
		}

		// if (LinearVelocity.Abs().z * 1000f < stopVelocityTreshold)
		// {
		// 	LinearVelocity = new Vector3(LinearVelocity.x, LinearVelocity.y, 0f);
		// }
		// if (LinearVelocity.Abs().x * 1000f < stopVelocityTreshold)
		// {
		// 	LinearVelocity = new Vector3(0f, LinearVelocity.y, LinearVelocity.z);
		// 	GD.Print("treshold reached");
		// }
	}

	public override void _Input(InputEvent inputEvent)
	{
		base._Input(inputEvent);

		// Mouse input
		if (inputEvent is InputEventMouseMotion)
		{
			InputEventMouseMotion inputEventMouseMotion = inputEvent as InputEventMouseMotion;

			camera.RotateX(Mathf.Deg2Rad(-inputEventMouseMotion.Relative.y * sensitivity.x));
			collisionShape.RotateY(Mathf.Deg2Rad(-inputEventMouseMotion.Relative.x * sensitivity.y));

			Vector3 cameraRotationClamped = camera.Rotation;
			cameraRotationClamped.x = Mathf.Clamp(cameraRotationClamped.x, -MaxRotationXDegrees, MaxRotationXDegrees);
			camera.Rotation = cameraRotationClamped;
		}

		base._Input(inputEvent);
	}

	private void BodyEnteredCallback(Node body)
	{
		jumpsLeft = maxJumps;
	}

	public Vector3 GlobalRotation()
	{
		return collisionShape.Rotation;
	}

	public Vector3 GetLinearVelocityLocal()
	{
		return LinearVelocity.Rotated(Vector3.Up, -GlobalRotation().y);
	}

	private void StartWallrun(bool leftSide, Vector3 normal)
	{
		if (TimeUntilNextWallrun > 0f)
		{
			// Wallrun is still on timeout
			return;
		}
		else if (TimeUntilNextWallrun <= 0f)
		{
			if (IsWallrunningRightSide || IsWallrunningLeftSide)
			{
				// Already wallrunning
				return;
			}
			else
			{
				// Not wallrunning yet, start wallrun
				float wallrunSideMultiplier = leftSide ? -1f : 1f;
				WallrunDirectionLastFrame = normal.Rotated(Vector3.Up, Mathf.Deg2Rad(90f * wallrunSideMultiplier));
				GravityScale = 0f;
				LinearVelocity = new Vector3(LinearVelocity.x, 0f, LinearVelocity.z);
				IsWallrunningLeftSide = leftSide;
				IsWallrunningRightSide = !leftSide;
				GD.Print("start wallrun");

				// Play effects and animations here
			}
		}
	}

	private void Wallrun(Vector3 wallNormal)
	{
		if (!IsWallrunningLeftSide && !IsWallrunningRightSide)
		{
			return;
		}

		float wallrunSideMultiplier = IsWallrunningLeftSide ? -1f : 1f;

		WallNormal = wallNormal;
		WallrunDirection = WallNormal.Rotated(Vector3.Up, Mathf.Deg2Rad(90f * wallrunSideMultiplier));
		WallrunDirectionChange = WallrunDirectionLastFrame - WallrunDirection;

		// TODO: Always add custom gravity for a small period of time after starting a wallrun
		if (GetLinearVelocityLocal().z <= 0f)
		{
			// Custom gravity
			AddConstantCentralForce(WallNormal * -GetLinearVelocityLocal().Abs().Length() * WallrunDirectionChange.Length() * 100000f);

			// Rotate camera along wall
			collisionShape.RotateY(Mathf.Deg2Rad(WallrunDirectionChange.Length() * GetLinearVelocityLocal().z * wallrunSideMultiplier * 5f));
		}
		else
		{
			// Custom gravity
			AddConstantCentralForce(WallNormal * -GetLinearVelocityLocal().Abs().Length() * WallrunDirectionChange.Length() * 100000f);

			// Rotate camera along wall
			collisionShape.RotateY(Mathf.Deg2Rad(WallrunDirectionChange.Length() * GetLinearVelocityLocal().z * wallrunSideMultiplier * 5f));
		}

		WallrunDirectionLastFrame = WallrunDirection;
	}

	private void StopWallrun()
	{
		if (IsWallrunningRightSide && !IsWallrunningLeftSide)
		{
			// Stop wallrunning on the right side
			TimeUntilNextWallrun = wallrunTimeout;
			WallrunDirectionChange = Vector3.Zero;
			GravityScale = defaultGravityScale;
			IsWallrunningRightSide = false;

			// Play effects and animations here

			GD.Print("stop wallrun");
		}
		else if (IsWallrunningLeftSide && !IsWallrunningRightSide)
		{
			// Stop wallrunning on the left side
			TimeUntilNextWallrun = wallrunTimeout;
			WallrunDirectionChange = Vector3.Zero;
			GravityScale = defaultGravityScale;
			IsWallrunningLeftSide = false;

			// Play effects and animations here

			GD.Print("stop wallrun");
		}
	}

	public void Jump()
	{
		if (jumpsLeft > 0)
		{
			if (IsWallrunningLeftSide || IsWallrunningRightSide)
			{
				StopWallrun();
				AddConstantCentralForce(WallNormal * 100000f);
				AddConstantCentralForce(collisionShape.GlobalTransform.basis.y * 25000f);
			}
			else
			{
				AddConstantCentralForce(collisionShape.GlobalTransform.basis.y * 25000f);
			}

			jumpsLeft -= 1;
		}
	}
}
