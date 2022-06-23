using Godot;

public class EntityHealth : Node
{
	[Export] public int Health { get; private set; } = 100;

	[Signal] public delegate void HealthChanged(int oldHealth, int newHealth, int difference);

	public override void _Process(float delta)
	{
		base._Process(delta);

		if (Input.IsKeyPressed((int)KeyList.T))
		{
			SetHealth(Health - 1);
		}
	}

	public void SetHealth(int newHealth)
	{
		int oldHealth = Health;
		Health = newHealth;

		EmitSignal(nameof(HealthChanged), oldHealth, Health, Health - oldHealth);
	}
}
