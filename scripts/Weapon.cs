using System;
using Godot;

public class Weapon : Spatial
{
	[Export] public string CameraNodePath = "/root/Spatial/Player/CollisionShape/Camera";
	[Export] public string HorizontalRotationNodePath = "/root/Spatial/Player/CollisionShape";
	[Export] public string RayCastNodePath = "Muzzle";
	[Export] public string ParticleSystemNodePath = "Muzzle/ParticleSystem";
	[Export] public Vector3 AimDownSightPosition;
	[Export] public float AimDownSightSpeed = 0.25f;
	[Export] public int ShotsPerSecond = 10;
	[Export] public float RecoilVerticalUpperLimitDegrees = 5f;
	[Export] public float RecoilVerticalLowerLimitDegrees = 2f;
	[Export] public float RecoilHorizontalUpperLimitDegrees = 1f;
	[Export] public float RecoilHorizontalLowerLimitDegrees = -1f;
	[Export] public int MagazineAmmoCount = 30;
	[Export] public int BarrelAmmoCount = 1;

	private Camera camera;
	private Spatial horizontalRotationNode;
	private RayCast rayCast;
	private Particles particleSystem;
	private Vector3 muzzlePosition;
	private Vector3 initialPosition;
	private float timeUntilNextShot;
	private int currentMagazineAmmoCount;
	private int currentBarrelAmmoCount;
	private RandomNumberGenerator rng;

	public override void _Ready()
	{
		camera = GetNode<Camera>(CameraNodePath);
		horizontalRotationNode = GetNode<Spatial>(HorizontalRotationNodePath);
		rayCast = GetNode<RayCast>(RayCastNodePath);
		particleSystem = GetNode<Particles>(ParticleSystemNodePath);
		muzzlePosition = rayCast.GlobalTransform.origin;
		initialPosition = Translation;
		currentMagazineAmmoCount = MagazineAmmoCount;
		currentBarrelAmmoCount = BarrelAmmoCount;
		rng = new RandomNumberGenerator();
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

		if ((Input.IsActionPressed("shoot") || Input.IsActionJustPressed("shoot")) && timeUntilNextShot <= 0)
		{
			Shoot();
		}

		if (Input.IsActionJustPressed("reload"))
		{
			Reload();
		}

		timeUntilNextShot -= delta;
	}

	private void Shoot()
	{
		// Time until next shot
		timeUntilNextShot = 1f / ShotsPerSecond;

		// Ammo
		if (currentMagazineAmmoCount <= 0 && currentBarrelAmmoCount > 0)
		{
			currentBarrelAmmoCount -= 1;
		}
		else if (currentMagazineAmmoCount > 0)
		{
			currentMagazineAmmoCount -= 1;
		}
		else
		{
			return;
		}

		// Recoil
		camera.RotateX(Mathf.Deg2Rad(rng.RandfRange(RecoilVerticalLowerLimitDegrees, RecoilVerticalUpperLimitDegrees)));
		horizontalRotationNode.RotateY(Mathf.Deg2Rad(rng.RandfRange(RecoilHorizontalLowerLimitDegrees, RecoilHorizontalUpperLimitDegrees)));

		// Muzzle flash
		particleSystem.Restart();

		// On hit
		if (rayCast.Enabled && rayCast.IsColliding())
		{
			GD.Print("hit something");

			// TODO: Muzzle flash, bullet impact hole, and bullet tracers
		}
	}

	private void Reload()
	{
		if (currentBarrelAmmoCount <= 0)
		{
			currentMagazineAmmoCount = MagazineAmmoCount - BarrelAmmoCount;
			currentBarrelAmmoCount = BarrelAmmoCount;
		}
		else if (currentMagazineAmmoCount <= 0)
		{
			currentMagazineAmmoCount = MagazineAmmoCount;
		}
	}
}
