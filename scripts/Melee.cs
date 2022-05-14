using System;
using Godot;

public partial class Melee : Node3D
{
	[Export] public readonly NodePath RayCastNode;
	[Export] public float MeleeTime = 1f;

	public delegate void MeleeHandler();
	[Signal] public event MeleeHandler Meleed;
	[Signal] public event MeleeHandler MeleedSuccessfully;

	private RayCast3D rayCast;

	public override void _Ready()
	{
		base._Ready();

		rayCast = GetNode<RayCast3D>(RayCastNode);
	}

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (Input.IsActionJustPressed("melee"))
		{
			Attack();
		}
	}

	public async void Attack()
	{
		Meleed.Invoke();

		if (!rayCast.Enabled)
		{
			return;
		}

		await ToSignal(GetTree().CreateTimer(MeleeTime), "timeout");
		if (rayCast.IsColliding())
		{
			MeleedSuccessfully.Invoke();
		}
	}
}
