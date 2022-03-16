using System;
using Godot;

public class PlayerController : RigidBody
{
	[Export] public float maxRotationXDegrees = 90f;
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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Connect("body_entered", this, nameof(_BodyEntered));

		camera = GetNode<Camera>("./CollisionShape/Camera");
		collisionShape = GetNode<CollisionShape>("./CollisionShape");

		jumpsLeft = maxJumps;

		Input.SetMouseMode(Input.MouseMode.Captured);
	}

	public override void _Process(float delta)
	{
		Vector3 globalRotation = collisionShape.GlobalTransform.basis.GetEuler();
		linearVelocityLocal = LinearVelocity.Rotated(Vector3.Up, -globalRotation.y);

		if (Input.IsActionJustReleased("restart"))
		{
			GetTree().ReloadCurrentScene();
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
				moveDirection += -collisionShape.GlobalTransform.basis.z * 1000f * accelerationMultiplier * (isSprinting ? 2f : 1f);
			}
			if (Input.IsActionPressed("move_backwards"))
			{
				moveDirection += collisionShape.GlobalTransform.basis.z * 1000f * accelerationMultiplier;
			}
			if (Input.IsActionPressed("move_right"))
			{
				moveDirection += collisionShape.GlobalTransform.basis.x * 1000f * accelerationMultiplier;
			}
			if (Input.IsActionPressed("move_left"))
			{
				moveDirection += -collisionShape.GlobalTransform.basis.x * 1000f * accelerationMultiplier;
			}
		}
		else if (isWallrunningRightSide || isWallrunningLeftSide)
		{
			if (Input.IsActionPressed("move_forward"))
			{
				moveDirection += -wallrunDirection * 1000f * accelerationMultiplier * (isSprinting ? 2f : 1f);
			}
			if (Input.IsActionPressed("move_backwards"))
			{
				moveDirection += wallrunDirection * 1000f * accelerationMultiplier;
			}
			if (Input.IsActionPressed("move_right"))
			{
				moveDirection += wallrunDirection * 1000f * accelerationMultiplier;
			}
			if (Input.IsActionPressed("move_left"))
			{
				moveDirection += -wallrunDirection * 1000f * accelerationMultiplier;
			}
		}

		// TODO: Fix broken deceleration
		// if (!Input.IsActionPressed("move_forward") && !Input.IsActionPressed("move_backwards"))
		// {
		// 	moveDirection = new Vector3(moveDirection.x, moveDirection.y, -LinearVelocityLocal().z * 1000f * deccelerationMultiplier);
		// }
		// if (!Input.IsActionPressed("move_right") && !Input.IsActionPressed("move_left"))
		// {
		// 	moveDirection = new Vector3(-LinearVelocityLocal().x * 1000f * deccelerationMultiplier, moveDirection.y, moveDirection.z);
		// }

		AddCentralForce(moveDirection);
	}

	public override void _PhysicsProcess(float delta)
	{
		PhysicsDirectSpaceState spaceState = GetWorld().DirectSpaceState;

		Vector3 globalRotation = collisionShape.GlobalTransform.basis.GetEuler();

		// Use global coordinates instead of local coordinates for raycasts
		// Right side raycast
		Godot.Collections.Dictionary raycastResultRight = spaceState.IntersectRay(GlobalTransform.origin,
			GlobalTransform.origin + new Vector3(5f, 0f, 0f).Rotated(Vector3.Up, globalRotation.y),
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
			GlobalTransform.origin + new Vector3(-5f, 0f, 0f).Rotated(Vector3.Up, globalRotation.y),
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

		if (timeUntilNextWallrun > 0f)
		{
			timeUntilNextWallrun -= delta;
			GD.Print("timeout -= delta");
		}
	}

	public override void _IntegrateForces(PhysicsDirectBodyState state)
	{
		if (LinearVelocity.Abs().x > maxVelocity)
		{
			LinearVelocity = new Vector3(maxVelocity * LinearVelocity.Normalized().x, LinearVelocity.y, LinearVelocity.z);
		}
		if (LinearVelocity.Abs().z > maxVelocity)
		{
			LinearVelocity = new Vector3(LinearVelocity.x, LinearVelocity.y, maxVelocity * LinearVelocity.Normalized().z);
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
		// Mouse input
		if (inputEvent is InputEventMouseMotion)
		{
			var inputEventMouseMotion = inputEvent as InputEventMouseMotion;

			camera.RotateX(Mathf.Deg2Rad(-inputEventMouseMotion.Relative.y * sensitivity.x));
			collisionShape.RotateY(Mathf.Deg2Rad(-inputEventMouseMotion.Relative.x * sensitivity.y));

			Vector3 cameraRotationClamped = camera.RotationDegrees;
			cameraRotationClamped.x = Mathf.Clamp(cameraRotationClamped.x, -maxRotationXDegrees, maxRotationXDegrees);
			camera.RotationDegrees = cameraRotationClamped;
		}

		base._Input(inputEvent);
	}

	private void _BodyEntered(Node body)
	{
		jumpsLeft = maxJumps;
	}

	public Vector3 LinearVelocityLocal()
	{
		Vector3 globalRotation = collisionShape.GlobalTransform.basis.GetEuler();
		return LinearVelocity.Rotated(Vector3.Up, -globalRotation.y);
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
				isWallrunningLeftSide = leftSide;
				isWallrunningRightSide = !leftSide;
				timeUntilNextWallrun = wallrunTimeout;
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

		if (linearVelocityLocal.z <= 0f)
		{
			// Custom gravity
			AddCentralForce(normal * linearVelocityLocal.z * 5000f);

			wallrunDirectionChange = wallrunDirectionLastFrame - wallrunDirection;
			GD.Print($"last frame: {wallrunDirectionLastFrame}, current frame: {wallrunDirection}, change: {wallrunDirectionChange}");
			collisionShape.RotateY(Mathf.Deg2Rad(wallrunDirectionChange.Length() * linearVelocityLocal.z * wallrunSideMultiplier * 5f));
			GD.Print(Mathf.Deg2Rad(wallrunDirectionChange.Length() * linearVelocityLocal.z * wallrunSideMultiplier * 5f));
		}
		else
		{
			// Custom gravity
			AddCentralForce(normal * wallrunDirectionChange.Length() * linearVelocityLocal.z * 5000f);

			wallrunDirectionChange = wallrunDirectionLastFrame - wallrunDirection;
			GD.Print($"last frame: {wallrunDirectionLastFrame}, current frame: {wallrunDirection}, change: {wallrunDirectionChange}");
			collisionShape.RotateY(Mathf.Deg2Rad(wallrunDirectionChange.Length() * linearVelocityLocal.z * wallrunSideMultiplier * 5f));
		}

		wallrunDirectionLastFrame = wallrunDirection;
	}

	private void StopWallrun(bool leftSide, Vector3 normal)
	{
		if (isWallrunningRightSide && !leftSide)
		{
			// Stop wallrunning on the right side
			wallrunDirectionChange = Vector3.Zero;
			GravityScale = 1f;
			isWallrunningRightSide = false;

			// Play effects and animations here

			GD.Print("stop wallrun");
		}
		else if (isWallrunningLeftSide && leftSide)
		{
			// Stop wallrunning on the left side
			wallrunDirectionChange = Vector3.Zero;
			GravityScale = 1f;
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
				AddCentralForce(wallNormal * 25000f);
			}
			else
			{
				AddCentralForce(collisionShape.GlobalTransform.basis.y * 25000f);
			}

			jumpsLeft -= 1;
		}
	}
}
