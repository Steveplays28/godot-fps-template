using System;
using Godot;

public class PlayerController : RigidBody
{
	[Export] public float maxRotationX = 90f;
	[Export] public Vector2 sensitivity = new Vector2(0.5f, 0.5f);

	public bool isSprinting { get; private set; } = false;

public Camera camera;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Input.SetMouseMode(Input.MouseMode.Captured);

		camera = GetNode<Camera>("Camera");
	}

	public override void _Process(float delta)
	{
		Vector3 globalRotation = GlobalTransform.basis.GetEuler();
		Vector3 linearVelocityLocal = LinearVelocity.Rotated(Vector3.Up, -globalRotation.y);

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
		if (Input.IsActionPressed("move_forward"))
		{
			moveDirection += -GlobalTransform.basis.z * 2500f * (isSprinting ? 2f : 1f);
		}
		if (Input.IsActionPressed("move_backwards"))
		{
			moveDirection += GlobalTransform.basis.z * 2500f;
		}
		if (Input.IsActionPressed("move_right"))
		{
			moveDirection += GlobalTransform.basis.x * 2500f;
		}
		if (Input.IsActionPressed("move_left"))
		{
			moveDirection += -GlobalTransform.basis.x * 2500f;
		}
		AddCentralForce(moveDirection);
	}

	public override void _Input(InputEvent inputEvent)
	{
		// Mouse input
		if (inputEvent is InputEventMouseMotion)
		{
			var inputEventMouseMotion = inputEvent as InputEventMouseMotion;

			camera.RotateX(Mathf.Deg2Rad(-inputEventMouseMotion.Relative.y * sensitivity.x));
			RotateY(Mathf.Deg2Rad(-inputEventMouseMotion.Relative.x * sensitivity.y));

			Vector3 newCameraRotation = camera.RotationDegrees;
			newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, -maxRotationX, maxRotationX);
			camera.RotationDegrees = newCameraRotation;
		}

		base._Input(inputEvent);
	}
}
