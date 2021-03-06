using System;
using Godot;
using SteveUtility;

public class Weapon : Spatial
{
	[Export] public NodePath PlayerNodePath;
	[Export] public NodePath CameraNodePath;
	[Export] public string UIManagerNodePath = "/root/UI";
	[Export] public NodePath HorizontalRotationNodePath;
	[Export] public NodePath RayCastNodePath = "Muzzle";
	[Export] public NodePath MuzzleFlashNodePath = "Muzzle/MuzzleFlash";
	[Export] public NodePath SmokeTrailNodePath = "Muzzle/SmokeTrail";
	[Export] public NodePath AnimationTreeNodePath = "AnimationTree";
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
	[Export] public float CrosshairFadeTime = 0.1f;

	private Spatial player;
	private Camera camera;
	private Control uiManager;
	private Spatial horizontalRotationNode;
	private RayCast rayCast;
	private Particles muzzleFlash;
	private Particles smokeTrail;
	private AnimationTree animationTree;
	private Vector3 muzzlePosition;
	private Vector3 initialPosition;
	private float timeUntilNextShot;
	private int currentMagazineAmmoCount;
	private int currentBarrelAmmoCount;
	private RandomNumberGenerator rng;
	private bool isAimingDownSight;
	private bool isReloading;
	private float originalCrosshairSelfModulateAlpha;

	public override void _Ready()
	{
		base._Ready();

		player = GetNode<Spatial>(PlayerNodePath);
		camera = GetNode<Camera>(CameraNodePath);
		uiManager = GetNode<Control>(UIManagerNodePath);
		horizontalRotationNode = GetNode<Spatial>(HorizontalRotationNodePath);
		rayCast = GetNode<RayCast>(RayCastNodePath);
		muzzleFlash = GetNode<Particles>(MuzzleFlashNodePath);
		smokeTrail = GetNodeOrNull<Particles>(SmokeTrailNodePath);
		animationTree = GetNode<AnimationTree>(AnimationTreeNodePath);
		muzzlePosition = rayCast.GlobalTransform.origin;
		initialPosition = Translation;
		currentMagazineAmmoCount = MagazineAmmoCount;
		currentBarrelAmmoCount = BarrelAmmoCount;
		rng = new RandomNumberGenerator();
		TextureRect crosshair = (TextureRect)uiManager.Get(nameof(UIManager.Crosshair));
		originalCrosshairSelfModulateAlpha = crosshair.SelfModulate.a;
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (Input.IsActionJustPressed("aim_down_sight") || Input.IsActionJustReleased("aim_down_sight"))
		{
			ToggleAimDownSight();
		}

		if (Input.IsActionPressed("shoot") || Input.IsActionJustPressed("shoot"))
		{
			Shoot();
		}

		if (Input.IsActionJustReleased("shoot"))
		{
			SmokeTrail();
		}

		if (Input.IsActionJustPressed("reload"))
		{
			Reload();
		}

		timeUntilNextShot -= delta;
	}

	public override void _PhysicsProcess(float delta)
	{
		base._PhysicsProcess(delta);

		SetCrosshairPosition();
	}

	private void ToggleAimDownSight()
	{
		Tween tween = this.CreateTween();

		if (isAimingDownSight)
		{
			tween.InterpolateProperty(this, "translation", null, initialPosition, AimDownSightSpeed);
			isAimingDownSight = false;
		}
		else
		{
			tween.InterpolateProperty(this, "translation", null, AimDownSightPosition, AimDownSightSpeed);
			isAimingDownSight = true;
		}

		tween.Start();
		tween.DeleteOnAllCompleted();
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
		float maxRotationXDegrees = (float)player.Get("MaxVerticalRotation");
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
		muzzleFlash.Restart();

		// On hit
		if (rayCast.Enabled && rayCast.IsColliding())
		{
			GD.Print("hit something");

			// TODO: Bullet impact hole, and bullet tracers
		}

		// Debug
		if (rayCast.Enabled)
		{
			Node lineDrawer = GetNode("/root/Debug/LineDrawer");

			if (rayCast.IsColliding())
			{
				lineDrawer.Call(nameof(LineDrawer.DrawLine), new[] { rayCast.GlobalTransform.origin, rayCast.GetCollisionPoint() }, new Color(255, 0, 0));
			}
			else
			{
				lineDrawer.Call(nameof(LineDrawer.DrawLine), new[] { rayCast.GlobalTransform.origin, ToGlobal(rayCast.CastTo) }, new Color(255, 0, 0));
			}
		}
	}

	private async void Reload()
	{
		if (currentMagazineAmmoCount != MagazineAmmoCount && !isReloading)
		{
			animationTree.Set("parameters/reload_one_shot/active", true);
			ToggleCrosshairVisibility();
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

			ToggleCrosshairVisibility();
			isReloading = false;
		}
	}

	private void SetCrosshairPosition()
	{
		if (rayCast.Enabled)
		{
			TextureRect crosshair = (TextureRect)uiManager.Get(nameof(UIManager.Crosshair));

			if (rayCast.IsColliding())
			{
				crosshair.RectPosition = camera.UnprojectPosition(rayCast.GetCollisionPoint()) - crosshair.RectSize / 2;
			}
			else
			{
				crosshair.RectPosition = camera.UnprojectPosition(ToGlobal(rayCast.CastTo)) - crosshair.RectSize / 2;
			}
		}
	}

	private void ToggleCrosshairVisibility()
	{
		TextureRect crosshair = (TextureRect)uiManager.Get(nameof(UIManager.Crosshair));
		Tween tween = this.CreateTween();

		if (crosshair.SelfModulate.a == 0f)
		{
			tween.InterpolateProperty(crosshair, "self_modulate", crosshair.SelfModulate, new Color(crosshair.SelfModulate.r, crosshair.SelfModulate.g, crosshair.SelfModulate.b, originalCrosshairSelfModulateAlpha), CrosshairFadeTime);
		}
		else
		{
			tween.InterpolateProperty(crosshair, "self_modulate", crosshair.SelfModulate, new Color(crosshair.SelfModulate.r, crosshair.SelfModulate.g, crosshair.SelfModulate.b, 0f), CrosshairFadeTime);
		}

		tween.Start();
		tween.DeleteOnAllCompleted();
	}

	public void SmokeTrail()
	{
		if (smokeTrail != null)
		{
			smokeTrail.Restart();
		}
	}
}
