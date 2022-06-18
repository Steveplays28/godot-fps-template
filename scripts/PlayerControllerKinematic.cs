using Godot;

public class PlayerControllerKinematic : KinematicBody
{
	[Export] public float MovementSpeed = 25f;
	[Export] public float MaxMovementSpeed = 25f;
	[Export] public float Mass = 80f;
	[Export] public Vector3 Gravity = new Vector3(0, -9.81f, 0);
	[Export] public float GravityScale = 1f;
	[Export(PropertyHint.Range, "0, 100")] public float Acceleration = 10f;
	[Export(PropertyHint.Range, "0, 100")] public float AirAcceleration = 5f;
	[Export(PropertyHint.Range, "0, 100")] public float Decceleration = 10f;
	[Export(PropertyHint.Range, "0, 100")] public float AirDecceleration = 5f;
	[Export] public Vector2 Sensitivity = new Vector2(1f, 1f);
	[Export] public float MaxVerticalRotation = 90f;
	[Export] public float JumpLength = 0.25f;
	[Export] public float JumpSpeed = 100f;
	[Export] private readonly NodePath FloorRayCastNodePath;
	[Export] private readonly NodePath CameraNodePath;

	public Vector3 Velocity { get; private set; } = Vector3.Zero;
	public Vector3 VelocityLocal { get; private set; } = Vector3.Zero;
	public bool IsJumping { get; private set; } = false;

	private RayCast floorRayCast;
	private Camera camera;
	private float jumpTimeLeft;
	private Vector3 targetVelocity;

	public override void _Ready()
	{
		floorRayCast = GetNode<RayCast>(FloorRayCastNodePath);
		camera = GetNode<Camera>(CameraNodePath);
	}

	public override void _PhysicsProcess(float delta)
	{
		base._PhysicsProcess(delta);

		HandleMouseCursorVisibilityInput();
		HandleRestartInput();

		HandleGravity(delta);
		HandleMovementInput(delta);
		HandleJumpInput(delta);
		ApplyVelocity(delta);
	}

	public override void _Input(InputEvent inputEvent)
	{
		base._Input(inputEvent);

		// Mouse input
		if (inputEvent is InputEventMouseMotion)
		{
			InputEventMouseMotion inputEventMouseMotion = inputEvent as InputEventMouseMotion;

			camera.RotateX(Mathf.Deg2Rad(-inputEventMouseMotion.Relative.y * Sensitivity.x));
			RotateY(Mathf.Deg2Rad(-inputEventMouseMotion.Relative.x * Sensitivity.y));

			Vector3 cameraRotationClamped = camera.RotationDegrees;
			cameraRotationClamped.x = Mathf.Clamp(cameraRotationClamped.x, -MaxVerticalRotation, MaxVerticalRotation);
			camera.RotationDegrees = cameraRotationClamped;
		}
	}

	public bool IsGrounded()
	{
		return floorRayCast.IsColliding();
	}

	private void HandleMouseCursorVisibilityInput()
	{
		if (Input.IsActionJustPressed("toggle_mouse_cursor_visibility"))
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
	}

	private void HandleRestartInput()
	{
		if (Input.IsActionJustPressed("restart"))
		{
			GetTree().ReloadCurrentScene();
		}
	}

	private void HandleGravity(float delta)
	{
		if (!IsGrounded())
		{
			targetVelocity += Gravity * Mass * GravityScale * delta;
		}
	}

	private void HandleMovementInput(float delta)
	{
		Vector3 inputDirection = Vector3.Zero;
		if (Input.IsActionPressed("move_forward"))
		{
			inputDirection -= Transform.basis.z;
		}
		if (Input.IsActionPressed("move_backwards"))
		{
			inputDirection += Transform.basis.z;
		}
		if (Input.IsActionPressed("move_right"))
		{
			inputDirection += Transform.basis.x;
		}
		if (Input.IsActionPressed("move_left"))
		{
			inputDirection -= Transform.basis.x;
		}
		inputDirection = inputDirection.Normalized();

		targetVelocity += inputDirection * MovementSpeed;

		float decceleration = IsGrounded() ? Decceleration : 0.5f;
		if (inputDirection.x == 0f)
		{
			targetVelocity = new Vector3(Mathf.Lerp(targetVelocity.x, 0f, decceleration * delta), targetVelocity.y, targetVelocity.z);
		}
		if (inputDirection.z == 0f)
		{
			targetVelocity = new Vector3(targetVelocity.x, targetVelocity.y, Mathf.Lerp(targetVelocity.z, 0f, decceleration * delta));
		}

		if (targetVelocity.Length() > MaxMovementSpeed)
		{
			targetVelocity = targetVelocity.Normalized() * MaxMovementSpeed;
		}
	}

	private void HandleJumpInput(float delta)
	{
		if (Input.IsActionJustPressed("jump"))
		{
			IsJumping = true;
			jumpTimeLeft = JumpLength;
		}

		if (IsJumping)
		{
			if (jumpTimeLeft > 0)
			{
				targetVelocity = new Vector3(targetVelocity.x, JumpSpeed * jumpTimeLeft, targetVelocity.z);
				jumpTimeLeft -= delta;
			}
			else
			{
				IsJumping = false;
			}
		}
	}

	private void ApplyVelocity(float delta)
	{
		float acceleration = IsGrounded() ? Acceleration : AirAcceleration;
		Velocity = new Vector3(Mathf.Lerp(Velocity.x, targetVelocity.x, acceleration * delta), targetVelocity.y, targetVelocity.z);
		Velocity = new Vector3(targetVelocity.x, targetVelocity.y, Mathf.Lerp(Velocity.z, targetVelocity.z, acceleration * delta));

		if (IsJumping)
		{
			Velocity = MoveAndSlide(Velocity, Transform.basis.y, true);
		}
		else
		{
			Velocity = MoveAndSlideWithSnap(Velocity, Transform.basis.y * -2f, Transform.basis.y, true);
		}
	}
}
