using System;
using Godot;

public class PlayerController : RigidBody
{
	[Export] public float maxRotationXDegrees = 90f;
	[Export] public Vector2 sensitivity = new Vector2(0.5f, 0.5f);
	[Export] public int wallrunShapeId = 1;

	public bool isSprinting { get; private set; } = false;
	public bool isWallrunning { get; private set; } = false;
	public Vector3 wallrunDirection { get; private set; } = Vector3.Zero;
	public Vector3 wallrunDirectionLastFrame { get; private set; } = Vector3.Zero;
	public Vector3 linearVelocityLocal { get; private set; } = Vector3.Zero;

	public Camera camera;
	public CollisionShape collisionShape;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Input.SetMouseMode(Input.MouseMode.Captured);

		camera = GetNode<Camera>("./CollisionShape/Camera");
		collisionShape = GetNode<CollisionShape>("./CollisionShape");
	}

	public override void _Process(float delta)
	{
		Vector3 globalRotation = collisionShape.GlobalTransform.basis.GetEuler();
		linearVelocityLocal = LinearVelocity.Rotated(Vector3.Up, -globalRotation.y);

		if (Input.IsActionJustReleased("restart"))
		{
			GetTree().ReloadCurrentScene();
		}

		Vector3 moveDirection = -LinearVelocity * 100f;
		isSprinting = false;
		if (Input.IsActionPressed("sprint"))
		{
			isSprinting = true;
		}

		if (!isWallrunning)
		{
			if (Input.IsActionPressed("move_forward"))
			{
				moveDirection += -collisionShape.GlobalTransform.basis.z * 2500f * (isSprinting ? 2f : 1f);
			}
			if (Input.IsActionPressed("move_backwards"))
			{
				moveDirection += collisionShape.GlobalTransform.basis.z * 2500f;
			}
			if (Input.IsActionPressed("move_right"))
			{
				moveDirection += collisionShape.GlobalTransform.basis.x * 2500f;
			}
			if (Input.IsActionPressed("move_left"))
			{
				moveDirection += -collisionShape.GlobalTransform.basis.x * 2500f;
			}
		}
		else
		{
			if (Input.IsActionPressed("move_forward"))
			{
				moveDirection += -wallrunDirection * 2500f * (isSprinting ? 2f : 1f);
			}
			if (Input.IsActionPressed("move_backwards"))
			{
				moveDirection += wallrunDirection * 2500f;
			}
			if (Input.IsActionPressed("move_right"))
			{
				moveDirection += wallrunDirection * 2500f;
			}
			if (Input.IsActionPressed("move_left"))
			{
				moveDirection += -wallrunDirection * 2500f;
			}
		}

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

		if (raycastResultRight.Count > 0)
		{
			GravityScale = 0f;
		}
		else
		{
			GravityScale = 1f;
			wallrunDirection = GlobalTransform.basis.z;
		}

		if (raycastResultRight.Contains("normal"))
		{
			Vector3 normalRight = (Vector3)raycastResultRight["normal"];
			wallrunDirection = normalRight.Rotated(Vector3.Up, Mathf.Deg2Rad(90f));
			isWallrunning = true;

			if (!wallrunDirection.IsEqualApprox(wallrunDirectionLastFrame))
			{
				collisionShape.RotateY(Mathf.Deg2Rad(0.05f * linearVelocityLocal.z));
			}
			else
			{
				wallrunDirectionLastFrame = normalRight.Rotated(Vector3.Up, Mathf.Deg2Rad(90f));
			}

			// Custom gravity
			AddCentralForce(-normalRight * 2500f);
		}
		else
		{
			isWallrunning = false;
		}

		// Left side raycast
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
}
