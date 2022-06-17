using Godot;
using SteveUtility;

public class PlayerController : RigidBody
{
	[Export] public float MaxRotationXDegrees = 90f;
	[Export] public Vector2 sensitivity = new Vector2(0.5f, 0.5f);
	[Export] public float accelerationMultiplier = 5f;
	[Export] public float deccelerationMultiplier = 1f;
	[Export] public float maxVelocity = 10f;
	[Export] public float stopVelocityTreshold = 1f;
	[Export] public int maxJumps = 2;
	[Export] public float wallrunTimeout = 2f;

	public bool isSprinting { get; private set; } = false;
	public bool isWallrunningRightSide { get; private set; } = false;
	public bool isWallrunningLeftSide { get; private set; } = false;
	public Vector3 wallrunDirection { get; private set; } = Vector3.Zero;
	public Vector3 wallrunDirectionLastFrame { get; private set; } = Vector3.Zero;
	public Vector3 wallrunDirectionChange { get; private set; } = Vector3.Zero;
	public Vector3 wallNormal { get; private set; } = Vector3.Zero;
	public float timeUntilNextWallrun { get; private set; } = 0f;
	public Vector3 linearVelocityLocal { get; private set; } = Vector3.Zero;
	public int jumpsLeft;

	public Camera camera;
	public CollisionShape collisionShape;

	private float defaultGravityScale;

	public override void _Ready()
	{
		base._Ready();

		Connect("body_entered", this, nameof(BodyEntered));

		camera = GetNode<Camera>("./CollisionShape/Camera");
		collisionShape = GetNode<CollisionShape>("./CollisionShape");
		defaultGravityScale = GravityScale;
		jumpsLeft = maxJumps;
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (Input.IsActionJustReleased("restart"))
		{
			_ = GetTree().ReloadCurrentScene();
			GetNode("/root/Debug/LineDrawer").Call(nameof(LineDrawer.ClearLines));
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
		isSprinting = false;
		if (Input.IsActionPressed("sprint"))
		{
			isSprinting = true;
		}

		if (!isWallrunningRightSide && !isWallrunningLeftSide)
		{
			if (Input.IsActionPressed("move_forward"))
			{
				moveDirection += -collisionShape.GlobalTransform.basis.z.Normalized() * 1000f * accelerationMultiplier * (isSprinting ? 2f : 1f);
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
		else if (isWallrunningRightSide || isWallrunningLeftSide)
		{
			if (Input.IsActionPressed("move_forward"))
			{
				moveDirection += -wallrunDirection * 4000f * accelerationMultiplier * (isSprinting ? 2f : 1f);
			}
			if (Input.IsActionPressed("move_backwards"))
			{
				moveDirection += wallrunDirection * 4000f * accelerationMultiplier;
			}
			if (Input.IsActionPressed("move_right"))
			{
				moveDirection += wallrunDirection * 4000f * accelerationMultiplier;
			}
			if (Input.IsActionPressed("move_left"))
			{
				moveDirection += -wallrunDirection * 4000f * accelerationMultiplier;
			}
		}

		if (!Input.IsActionPressed("move_forward") && !Input.IsActionPressed("move_backwards"))
		{
			AddCentralForce(-LinearVelocityLocal().z * collisionShape.GlobalTransform.basis.z * 1000f * deccelerationMultiplier);
		}
		if (!Input.IsActionPressed("move_right") && !Input.IsActionPressed("move_left"))
		{
			AddCentralForce(-LinearVelocityLocal().x * collisionShape.GlobalTransform.basis.x * 1000f * deccelerationMultiplier);
		}

		AddCentralForce(moveDirection);
	}

	public override void _PhysicsProcess(float delta)
	{
		base._PhysicsProcess(delta);

		PhysicsDirectSpaceState spaceState = GetWorld().DirectSpaceState;

		// Use global coordinates instead of local coordinates for raycasts
		// Right side raycast
		Godot.Collections.Dictionary raycastResultRight = spaceState.IntersectRay(GlobalTransform.origin,
			GlobalTransform.origin + new Vector3(5f, 0f, 0f).Rotated(Vector3.Up, GlobalRotation().y),
			new Godot.Collections.Array { this });

		Vector3 normalRight = Vector3.Zero;
		if (raycastResultRight.Contains("normal"))
		{
			normalRight = (Vector3)raycastResultRight["normal"];
			StartWallrun(false, normalRight);
			Wallrun(false, normalRight);
		}
		else
		{
			StopWallrun(false, normalRight);
		}

		// Left side raycast
		Godot.Collections.Dictionary raycastResultLeft = spaceState.IntersectRay(GlobalTransform.origin,
			GlobalTransform.origin + new Vector3(-5f, 0f, 0f).Rotated(Vector3.Up, GlobalRotation().y),
			new Godot.Collections.Array { this });

		Vector3 normalLeft = Vector3.Zero;
		if (raycastResultLeft.Contains("normal"))
		{
			normalLeft = (Vector3)raycastResultLeft["normal"];
			StartWallrun(true, normalLeft);
			Wallrun(true, normalLeft);
		}
		else
		{
			StopWallrun(true, normalLeft);
		}

		if (!isWallrunningLeftSide && !isWallrunningRightSide && timeUntilNextWallrun > 0f)
		{
			timeUntilNextWallrun -= delta;
		}
	}

	public override void _IntegrateForces(PhysicsDirectBodyState state)
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

			Vector3 cameraRotationClamped = camera.RotationDegrees;
			cameraRotationClamped.x = Mathf.Clamp(cameraRotationClamped.x, -MaxRotationXDegrees, MaxRotationXDegrees);
			camera.RotationDegrees = cameraRotationClamped;
		}
	}

#pragma warning disable
	private void BodyEntered(Node body)
#pragma warning restore
	{
		jumpsLeft = maxJumps;
	}

	public Vector3 GlobalRotation()
	{
		return collisionShape.Rotation;
	}

	public Vector3 LinearVelocityLocal()
	{
		return LinearVelocity.Rotated(Vector3.Up, -GlobalRotation().y);
	}

	private void StartWallrun(bool leftSide, Vector3 normal)
	{
		if (timeUntilNextWallrun > 0f)
		{
			// Wallrun is still on timeout
			return;
		}
		else if (timeUntilNextWallrun <= 0f)
		{
			if (isWallrunningRightSide || isWallrunningLeftSide)
			{
				// Already wallrunning
				return;
			}
			else
			{
				// Not wallrunning yet, start wallrun
				float wallrunSideMultiplier = leftSide ? -1f : 1f;
				wallrunDirectionLastFrame = normal.Rotated(Vector3.Up, Mathf.Deg2Rad(90f * wallrunSideMultiplier));
				GravityScale = 0f;
				LinearVelocity = new Vector3(LinearVelocity.x, 0f, LinearVelocity.z);
				isWallrunningLeftSide = leftSide;
				isWallrunningRightSide = !leftSide;
				GD.Print("start wallrun");

				// Play effects and animations here
			}
		}
	}

	private void Wallrun(bool leftSide, Vector3 normal)
	{
		if (!isWallrunningLeftSide && !isWallrunningRightSide)
		{
			return;
		}

		float wallrunSideMultiplier = leftSide ? -1f : 1f;

		wallNormal = normal;
		wallrunDirection = normal.Rotated(Vector3.Up, Mathf.Deg2Rad(90f * wallrunSideMultiplier));
		wallrunDirectionChange = wallrunDirectionLastFrame - wallrunDirection;

		// TODO: Always add custom gravity for a small period of time after starting a wallrun
		if (LinearVelocityLocal().z <= 0f)
		{
			// Custom gravity
			AddCentralForce(normal * -LinearVelocityLocal().Abs().Length() * wallrunDirectionChange.Length() * 100000f);

			// Rotate camera along wall
			collisionShape.RotateY(Mathf.Deg2Rad(wallrunDirectionChange.Length() * LinearVelocityLocal().z * wallrunSideMultiplier * 5f));
		}
		else
		{
			// Custom gravity
			AddCentralForce(normal * -LinearVelocityLocal().Abs().Length() * wallrunDirectionChange.Length() * 100000f);

			// Rotate camera along wall
			collisionShape.RotateY(Mathf.Deg2Rad(wallrunDirectionChange.Length() * LinearVelocityLocal().z * wallrunSideMultiplier * 5f));
		}

		wallrunDirectionLastFrame = wallrunDirection;
	}

	private void StopWallrun(bool leftSide, Vector3 normal)
	{
		if (isWallrunningRightSide && !leftSide)
		{
			// Stop wallrunning on the right side
			timeUntilNextWallrun = wallrunTimeout;
			wallrunDirectionChange = Vector3.Zero;
			GravityScale = defaultGravityScale;
			isWallrunningRightSide = false;

			// Play effects and animations here

			GD.Print("stop wallrun");
		}
		else if (isWallrunningLeftSide && leftSide)
		{
			// Stop wallrunning on the left side
			timeUntilNextWallrun = wallrunTimeout;
			wallrunDirectionChange = Vector3.Zero;
			GravityScale = defaultGravityScale;
			isWallrunningLeftSide = false;

			// Play effects and animations here

			GD.Print("stop wallrun");
		}
	}

	public void Jump()
	{
		if (jumpsLeft > 0)
		{
			if (isWallrunningLeftSide || isWallrunningRightSide)
			{
				StopWallrun(isWallrunningLeftSide, wallNormal);
				AddCentralForce(wallNormal * 100000f);
				AddCentralForce(collisionShape.GlobalTransform.basis.y * 25000f);
			}
			else
			{
				AddCentralForce(collisionShape.GlobalTransform.basis.y * 25000f);
			}

			jumpsLeft -= 1;
		}
	}
}
