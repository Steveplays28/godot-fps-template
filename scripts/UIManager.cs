using System;
using Godot;

public class UIManager : Control
{
	[Export] readonly bool enableDebug = true;

	public override void _Ready()
	{
		if (!enableDebug)
		{
			SetDebugLabelText("");
		}
	}

	public override void _Process(float delta)
	{
		if (enableDebug)
		{
			RigidBody player = GetNode<RigidBody>("/root/Spatial/Player");
			SetDebugLabelText($"FPS: {1 / delta}\n\nGlobal position: {player.GlobalTransform.origin}\nGlobal linear velocity: {player.LinearVelocity}\n\nLocal linear velocity: {player.Call(nameof(PlayerController.LinearVelocityLocal))}");
		}
	}

	public void SetDebugLabelText(string text)
	{
		GetNode<Label>("./Label").Text = text;
	}
}
