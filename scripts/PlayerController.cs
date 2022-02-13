using System;
using Godot;

public class PlayerController : RigidBody
{
	[Export] public float maxRotationX = 90f;
	[Export] public Vector2 sensitivity = new Vector2(0.5f, 0.5f);

	public Camera camera;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Input.SetMouseMode(Input.MouseMode.Captured);

		camera = GetNode<Camera>("Camera");
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
