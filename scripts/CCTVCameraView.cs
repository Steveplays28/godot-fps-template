using System;
using Godot;

public class CCTVCameraView : Sprite3D
{
	[Export] public readonly NodePath CCTVCameraNodePath;
	private Node cctvCamera;
	private Viewport cctvCameraViewport;

	public override void _Ready()
	{
		base._Ready();

		cctvCamera = GetNodeOrNull(CCTVCameraNodePath);
		if (cctvCamera != null)
		{
			cctvCameraViewport = cctvCamera.GetNodeOrNull<Viewport>("Viewport");
		}
		if (cctvCameraViewport != null)
		{
			Texture texture = cctvCameraViewport.GetTexture();
			texture.Flags = (uint)Texture.FlagsEnum.Filter;
			Texture = texture;
			GD.Print("e");
		}
	}
}
