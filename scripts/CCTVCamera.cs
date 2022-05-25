using System;
using Godot;

public class CCTVCamera : Spatial
{
	[Export] public readonly NodePath CameraNodePath;
	[Export] public readonly Vector3 CameraPosition;
	private Camera camera;

	public override void _Ready()
	{
		base._Ready();

		camera = GetNodeOrNull<Camera>(CameraNodePath);
		if (camera != null)
		{
			camera.GlobalTransform = new Transform(camera.GlobalTransform.basis, GlobalTransform.origin + CameraPosition);
		}
	}
}
