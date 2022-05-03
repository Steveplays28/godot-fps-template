using System;
using Godot;
using SteveUtility;

public class UIManager : Control
{
	[Export] public bool IsNonDebugUIVisible = true;
	[Export] public bool IsDebugUIVisible = true;

	public Control NonDebugUI;
	public Control DebugUI;
	public Label DebugLabel;
	public TextureRect Crosshair;

	public override void _Ready()
	{
		base._Ready();

		NonDebugUI = GetNode<Control>("NonDebug");
		DebugUI = GetNode<Control>("Debug");
		DebugLabel = DebugUI.GetNode<Label>("DebugLabel");
		Crosshair = NonDebugUI.GetNode<TextureRect>("Crosshair");

		if (IsNonDebugUIVisible)
		{
			NonDebugUI.Modulate = new Color(NonDebugUI.Modulate.r, NonDebugUI.Modulate.g, NonDebugUI.Modulate.b, 1f);
		}
		else
		{
			NonDebugUI.Modulate = new Color(NonDebugUI.Modulate.r, NonDebugUI.Modulate.g, NonDebugUI.Modulate.b, 0f);
		}

		if (IsDebugUIVisible)
		{
			DebugUI.Modulate = new Color(DebugUI.Modulate.r, DebugUI.Modulate.g, DebugUI.Modulate.b, 1f);
		}
		else
		{
			DebugUI.Modulate = new Color(DebugUI.Modulate.r, DebugUI.Modulate.g, DebugUI.Modulate.b, 0f);
		}
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (IsDebugUIVisible)
		{
			RigidBody player = GetNode<RigidBody>("/root/Spatial/Player");
			DebugLabel.Text = $"FPS: {1 / delta}\n\nGlobal position: {player.GlobalTransform.origin}\nGlobal linear velocity: {player.LinearVelocity}\n\nLocal linear velocity: {player.Call(nameof(PlayerController.LinearVelocityLocal))}";
		}

		if (Input.IsActionJustPressed("toggle_non_debug_ui_visibility"))
		{
			ToggleNonDebugUIVisibility();
		}

		if (Input.IsActionJustPressed("toggle_debug_ui_visibility"))
		{
			ToggleDebugUIVisibility();
		}
	}

	public void ToggleNonDebugUIVisibility()
	{
		if (IsNonDebugUIVisible)
		{
			NonDebugUI.Modulate = new Color(NonDebugUI.Modulate.r, NonDebugUI.Modulate.g, NonDebugUI.Modulate.b, 0f);
			IsNonDebugUIVisible = false;
		}
		else
		{
			NonDebugUI.Modulate = new Color(NonDebugUI.Modulate.r, NonDebugUI.Modulate.g, NonDebugUI.Modulate.b, 1f);
			IsNonDebugUIVisible = true;
		}
	}

	public void ToggleDebugUIVisibility()
	{
		if (IsDebugUIVisible)
		{
			DebugUI.Modulate = new Color(DebugUI.Modulate.r, DebugUI.Modulate.g, DebugUI.Modulate.b, 0f);
			GetNode("/root/Debug/LineDrawer").Call(nameof(LineDrawer.ClearLines));
			IsDebugUIVisible = false;
		}
		else
		{
			DebugUI.Modulate = new Color(DebugUI.Modulate.r, DebugUI.Modulate.g, DebugUI.Modulate.b, 1f);
			IsDebugUIVisible = true;
		}
	}
}
