using System;
using Godot;

namespace SteveUtility
{
	public static class TweenExtensions
	{
		public static Tween CreateTween(this Node node)
		{
			Tween tween = new Tween();
			node.AddChild(tween);

			return tween;
		}

		public static async void DeleteOnAllCompleted(this Tween tween)
		{
			await tween.ToSignal(tween, "tween_all_completed");
			tween.QueueFree();
		}
	}
}
