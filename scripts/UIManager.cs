using System;
using Godot;

public class UIManager : Control
{
	[Export] bool enableDebug = true;

	public override void _Process(float delta)
	{
		if (enableDebug)
		{
			RigidBody player = GetNode<RigidBody>("/root/Spatial/Player");
			SetDebugLabelText($"Global position: {player.GlobalTransform.origin}\nGlobal linear velocity: {player.LinearVelocity}");
		}
	}

	public void SetDebugLabelText(string text)
	{
		GetNode<Label>("./Label").Text = text;
	}
}
