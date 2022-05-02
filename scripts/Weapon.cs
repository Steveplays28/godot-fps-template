using System;
using Godot;

public class Weapon : Spatial
{
	[Export] public string PlayerNodePath = "/root/Spatial/Player";
	[Export] public string CameraNodePath = "/root/Spatial/Player/CollisionShape/Camera";
	[Export] public string HorizontalRotationNodePath = "/root/Spatial/Player/CollisionShape";
	[Export] public string RayCastNodePath = "Muzzle";
	[Export] public string ParticleSystemNodePath = "Muzzle/ParticleSystem";
	[Export] public string AnimationTreeNodePath = "AnimationTree";
	[Export] public Vector3 AimDownSightPosition;
	[Export] public float AimDownSightSpeed = 0.25f;
	[Export] public int ShotsPerSecond = 10;
	[Export] public float RecoilVerticalUpperLimitDegrees = 5f;
	[Export] public float RecoilVerticalLowerLimitDegrees = 2f;
	[Export] public float RecoilHorizontalUpperLimitDegrees = 1f;
	[Export] public float RecoilHorizontalLowerLimitDegrees = -1f;
	[Export] public int MagazineAmmoCount = 30;
	[Export] public int BarrelAmmoCount = 1;
	[Export] public float ReloadTime = 2f;

	private Spatial player;
	private Camera camera;
	private Spatial horizontalRotationNode;
	private RayCast rayCast;
	private Particles particleSystem;
	private AnimationTree animationTree;
	private Vector3 muzzlePosition;
	private Vector3 initialPosition;
	private float timeUntilNextShot;
	private int currentMagazineAmmoCount;
	private int currentBarrelAmmoCount;
	private RandomNumberGenerator rng;
	private bool isReloading;

	public override void _Ready()
	{
		player = GetNode<Spatial>(PlayerNodePath);
		camera = GetNode<Camera>(CameraNodePath);
		horizontalRotationNode = GetNode<Spatial>(HorizontalRotationNodePath);
		rayCast = GetNode<RayCast>(RayCastNodePath);
		particleSystem = GetNode<Particles>(ParticleSystemNodePath);
		animationTree = GetNode<AnimationTree>(AnimationTreeNodePath);
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
			AimDownSight();
		}

		if (Input.IsActionPressed("shoot") || Input.IsActionJustPressed("shoot"))
		{
			Shoot();
		}

		if (Input.IsActionJustPressed("reload"))
		{
			Reload();
		}

		timeUntilNextShot -= delta;
	}

	private async void AimDownSight()
	{
		Tween tween = new Tween();
		AddChild(tween);
		tween.InterpolateProperty(this, "translation", null, initialPosition, AimDownSightSpeed);
		tween.Start();

		await ToSignal(tween, "tween_all_completed");
		tween.QueueFree();
	}

	private void Shoot()
	{
		if (timeUntilNextShot > 0 || isReloading)
		{
			return;
		}

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

		// Vertical recoil
		float maxRotationXDegrees = (float)player.Get("MaxRotationXDegrees");
		float recoilVertical = rng.RandfRange(RecoilVerticalLowerLimitDegrees, RecoilVerticalUpperLimitDegrees);
		if (recoilVertical >= 0)
		{
			if (camera.RotationDegrees.x <= maxRotationXDegrees && camera.RotationDegrees.x + recoilVertical <= maxRotationXDegrees)
			{
				camera.RotateX(Mathf.Deg2Rad(recoilVertical));
			}
		}
		else
		{
			if (camera.RotationDegrees.x >= -maxRotationXDegrees && camera.RotationDegrees.x + recoilVertical >= -maxRotationXDegrees)
			{
				camera.RotateX(Mathf.Deg2Rad(recoilVertical));
			}
		}

		// Horizontal recoil
		float recoilHorizontal = rng.RandfRange(RecoilHorizontalLowerLimitDegrees, RecoilHorizontalUpperLimitDegrees);
		horizontalRotationNode.RotateY(Mathf.Deg2Rad(recoilHorizontal));

		// Muzzle flash
		particleSystem.Restart();

		// On hit
		if (rayCast.Enabled && rayCast.IsColliding())
		{
			GD.Print("hit something");

			// TODO: Bullet impact hole, and bullet tracers
		}
	}

	private async void Reload()
	{
		if (currentMagazineAmmoCount != MagazineAmmoCount)
		{
			animationTree.Set("parameters/reload_one_shot/active", true);
			isReloading = true;
			await ToSignal(GetTree().CreateTimer(ReloadTime), "timeout");

			if (currentBarrelAmmoCount <= 0)
			{
				currentMagazineAmmoCount = MagazineAmmoCount - BarrelAmmoCount;
				currentBarrelAmmoCount = BarrelAmmoCount;
			}
			else
			{
				currentMagazineAmmoCount = MagazineAmmoCount;
			}

			isReloading = false;
		}
	}
}
