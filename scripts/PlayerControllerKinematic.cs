using Godot;

public class PlayerControllerKinematic : KinematicBody
{
	[Export] public float MovementSpeed = 25f;
	[Export] public float Mass = 80f;
	[Export] public Vector3 Gravity = new Vector3(0, -9.81f, 0);
	[Export] public float GravityScale = 1f;
	[Export(PropertyHint.Range, "0, 1")] public float Friction = 0.5f;
	[Export] public Vector2 Sensitivity = new Vector2(1f, 1f);
	[Export] public float MaxVerticalRotation = 90f;
	[Export] private readonly NodePath FloorRayCastNodePath;
	[Export] private readonly NodePath CameraNodePath;

	public Vector3 Velocity { get; private set; } = Vector3.Zero;
	public Vector3 VelocityLocal { get; private set; } = Vector3.Zero;

	private RayCast floorRayCast;
	private Camera camera;

	public override void _Ready()
	{
		floorRayCast = GetNode<RayCast>(FloorRayCastNodePath);
		camera = GetNode<Camera>(CameraNodePath);
	}

	public override void _PhysicsProcess(float delta)
	{
		base._PhysicsProcess(delta);

		ApplyMouseCursorVisibility();

		GetInput();
		ApplyGravity(delta);
		ApplyMovement();
		ApplyJumping();
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

	private void ApplyMouseCursorVisibility()
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

	private void GetInput()
	{
		Vector2 inputDirection = Vector2.Zero;
		if (Input.IsActionPressed("move_forward"))
		{
			inputDirection.y += 1f;
		}
		if (Input.IsActionPressed("move_backwards"))
		{
			inputDirection.y -= 1f;
		}
		if (Input.IsActionPressed("move_right"))
		{
			inputDirection.x += 1f;
		}
		if (Input.IsActionPressed("move_left"))
		{
			inputDirection.x -= 1f;
		}

		if (inputDirection.x != 0f)
		{
			Velocity = new Vector3(Mathf.Lerp(Velocity.x, inputDirection.x * MovementSpeed, Friction), Velocity.y, Velocity.z);
		}
		else
		{
			Velocity = new Vector3(Mathf.Lerp(Velocity.x, 0f, Friction), Velocity.y, Velocity.z);
		}
		if (inputDirection.y != 0f)
		{
			Velocity = new Vector3(Velocity.x, Velocity.y, Mathf.Lerp(Velocity.z, inputDirection.y * MovementSpeed * -1, Friction));
		}
		else
		{
			Velocity = new Vector3(Velocity.x, Velocity.y, Mathf.Lerp(Velocity.z, 0f, Friction));
		}
	}

	private void ApplyGravity(float delta)
	{
		if (!IsGrounded())
		{
			Velocity += Gravity * Mass * GravityScale * delta;
			Velocity = MoveAndSlide(Velocity, Transform.basis.y, true);
		}
		else if (Velocity.y < 0f)
		{
			Velocity = new Vector3(Velocity.x, 0f, Velocity.z);
		}
	}

	private void ApplyMovement()
	{
		if (IsGrounded())
		{
			MoveAndSlideWithSnap(Velocity, Vector3.Up, Transform.basis.y, true);
		}
	}

	private void ApplyJumping()
	{
		
	}
}
