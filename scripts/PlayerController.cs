using System;
using Godot;

public class PlayerController : RigidBody
{
	#region Variables - references
	private Camera camera;
	#endregion

	#region Variables - mouse
	public float sensX = 0.01f;
	public float sensY = 0.01f;
	public float maxRotationY = 90f;

	private float mouseMovementX;
	private float mouseMovementY;
	#endregion

	#region Variables - movement
	public Vector3 linearVelocityLocal;
	private float[] movementSpeed = new float[] { 100000, 100000, 50000, 50000 };
	private float stopSpeed = 10000;
	private float jumpForce = 250;
	#endregion

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Input.SetMouseMode(Input.MouseMode.Captured);

		camera = GetNode<Camera>("Camera");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(float delta)
	{
		// Convert linear velocity from global to local
		linearVelocityLocal = Transform.basis.Orthonormalized().XformInv(LinearVelocity);

		// Mouse Y
		var e = maxRotationY - mouseMovementY * sensY;
		if (e < mouseMovementY * sensY)
		{
			mouseMovementY = e;
		}

		if (camera.RotationDegrees.x > -maxRotationY && camera.RotationDegrees.x < maxRotationY)
		{
			camera.GlobalRotate(Transform.basis.x, -mouseMovementY * sensY);
		}
		else if (camera.RotationDegrees.x < -maxRotationY && -mouseMovementY > 0)
		{
			camera.GlobalRotate(Transform.basis.x, -mouseMovementY * sensY);
		}
		else if (camera.RotationDegrees.x > maxRotationY && -mouseMovementY < 0)
		{
			camera.GlobalRotate(Transform.basis.x, -mouseMovementY * sensY);
		}
		mouseMovementY = 0;

		camera.RotationDegrees = new Vector3(camera.RotationDegrees.x, camera.RotationDegrees.y, Mathf.Clamp(-linearVelocityLocal.x, -5f, 5f));

		if (Input.IsActionJustPressed("restart"))
		{
			var timeBeforeSceneChange = OS.GetTicksMsec();
			// GetTree().ChangeScene("res://main_scene.tscn");
			GetTree().ReloadCurrentScene();
			var timeAfterSceneChange = OS.GetTicksMsec();

			GD.Print("Scene reloaded in " + (timeAfterSceneChange - timeBeforeSceneChange).ToString() + "ms");
		}
		if (Input.IsActionJustPressed("escape"))
		{
			Input.SetMouseMode(Input.GetMouseMode() == Input.MouseMode.Visible ? Input.MouseMode.Hidden : Input.MouseMode.Visible);
		}
	}

	public override void _PhysicsProcess(float delta)
	{
		// Movement
		// if (!Input.IsActionPressed("move_forward") && !Input.IsActionPressed("move_backwards"))
		// {
		// 	if (linearVelocityLocal.z > 0)
		// 	{
		// 		AddForce(Vector3.Back * linearVelocityLocal.z * stopSpeed * delta, Vector3.Zero);
		// 	}
		// 	else if (linearVelocityLocal.z < 0)
		// 	{
		// 		AddForce(Vector3.Forward * -linearVelocityLocal.z * stopSpeed * delta, Vector3.Zero);
		// 	}
		// }
		// if (!Input.IsActionPressed("move_right") && !Input.IsActionPressed("move_left"))
		// {
		// 	if (linearVelocityLocal.x > 0)
		// 	{
		// 		AddForce(Vector3.Left * linearVelocityLocal.x * stopSpeed * delta, Vector3.Zero);
		// 	}
		// 	else if (linearVelocityLocal.x < 0)
		// 	{
		// 		AddForce(Vector3.Right * -linearVelocityLocal.x * stopSpeed * delta, Vector3.Zero);
		// 	}
		// }

		if (Input.IsActionPressed("move_forward"))
		{
			AddForce(-Transform.basis.z * movementSpeed[0] * delta, Vector3.Zero);
		}
		if (Input.IsActionPressed("move_backwards"))
		{
			AddForce(Transform.basis.z * movementSpeed[1] * delta, Vector3.Zero);
		}
		if (Input.IsActionPressed("move_right"))
		{
			AddForce(Transform.basis.x * movementSpeed[2] * delta, Vector3.Zero);
		}
		if (Input.IsActionPressed("move_left"))
		{
			AddForce(-Transform.basis.x * movementSpeed[3] * delta, Vector3.Zero);
		}

		if (Input.IsActionJustPressed("jump"))
		{
			ApplyImpulse(Vector3.Zero, Transform.basis.y * jumpForce);
		}

		base._PhysicsProcess(delta);
	}

	public override void _IntegrateForces(PhysicsDirectBodyState state)
	{
		// Mouse X
		state.AngularVelocity = -mouseMovementX * sensX * Transform.basis.y * 100;
		mouseMovementX = 0;

		base._IntegrateForces(state);
	}

	public override void _Input(InputEvent inputEvent)
	{
		// Mouse input
		if (inputEvent is InputEventMouseMotion)
		{
			var inputEventMouseMotion = inputEvent as InputEventMouseMotion;

			mouseMovementX = inputEventMouseMotion.Relative.x;
			mouseMovementY = inputEventMouseMotion.Relative.y;
		}

		base._Input(inputEvent);
	}

	private void InfiniteMouse()
	{
		// Mouse X
		if (GetViewport().GetMousePosition().x >= GetViewport().Size.x)
		{
			GetViewport().WarpMouse(new Vector2(0, GetViewport().GetMousePosition().y));
		}
		else if (GetViewport().GetMousePosition().x <= 0)
		{
			GetViewport().WarpMouse(new Vector2(GetViewport().Size.x, GetViewport().GetMousePosition().y));
		}

		// Mouse Y
		if (GetViewport().GetMousePosition().y >= GetViewport().Size.y)
		{
			GetViewport().WarpMouse(new Vector2(GetViewport().GetMousePosition().x, 0));
		}
		else if (GetViewport().GetMousePosition().y <= 0)
		{
			GetViewport().WarpMouse(new Vector2(GetViewport().GetMousePosition().x, GetViewport().Size.y));
		}
	}
}
