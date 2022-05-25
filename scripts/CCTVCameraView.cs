using System;
using Godot;

public class CCTVCameraView : Sprite3D
{
	[Export] public readonly NodePath CCTVCameraNodePath;
	private Node CCTVCamera;
	private Viewport CCTVCameraViewport;

	public override void _Ready()
	{
		base._Ready();

		CCTVCamera = GetNodeOrNull(CCTVCameraNodePath);
		if (CCTVCamera != null)
		{
			CCTVCameraViewport = CCTVCamera.GetNodeOrNull<Viewport>("Viewport");
		}
		if (CCTVCameraViewport != null)
		{
			Texture texture = CCTVCameraViewport.GetTexture();
			texture.Flags = (uint)Texture.FlagsEnum.Filter;
			Texture = texture;
		}
	}
}
