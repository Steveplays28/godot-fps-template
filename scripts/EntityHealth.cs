using Godot;

public class EntityHealth : Node
{
	[Export] public int Health = 100;

	[Signal] public delegate void HealthChanged(int oldHealth, int newHealth, int difference);

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (Input.IsKeyPressed((int)KeyList.T))
		{
			int oldHealth = Health;
			Health -= 1;
			EmitSignal(nameof(HealthChanged), oldHealth, Health, oldHealth - Health);
		}
	}
}
