using System;
using Godot;

public class Weapon : Spatial
{
	[Export] public string rayCastNodePath = "RayCast";
	[Export] public Vector3 AimDownSightPosition;
	[Export] public float AimDownSightSpeed = 0.25f;

	private RayCast rayCast;
	private Vector3 muzzlePosition;
	private Vector3 initialPosition;

	public override void _Ready()
	{
		rayCast = GetNode<RayCast>(rayCastNodePath);
		muzzlePosition = rayCast.GlobalTransform.origin;
		initialPosition = Translation;
	}

	public override void _Process(float delta)
	{
		if (Input.IsActionJustPressed("aim_down_sight"))
		{
			Tween tween = new Tween();
			AddChild(tween);
			tween.InterpolateProperty(this, "translation", null, AimDownSightPosition, AimDownSightSpeed);
			tween.Start();
		}

		if (Input.IsActionJustReleased("aim_down_sight"))
		{
			Tween tween = new Tween();
			AddChild(tween);
			tween.InterpolateProperty(this, "translation", null, initialPosition, AimDownSightSpeed);
			tween.Start();
		}

		if (Input.IsActionJustPressed("shoot"))
		{
			if (rayCast.Enabled && rayCast.IsColliding())
			{
				GD.Print("hit something");

				// TODO: Muzzle flash, bullet impact hole, and bullet tracers
			}
		}
	}
}
