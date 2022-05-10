using Godot;

namespace SteveUtility
{
	public partial class LineDrawer : ImmediateMesh
	{
		public void DrawLine(Vector3[] points)
		{
			SurfaceBegin(PrimitiveType.Lines);
			for (int i = 0; i < points.Length; ++i)
			{
				SurfaceAddVertex(points[i]);
			}
			SurfaceEnd();
		}
		public void DrawLine(Vector3[] points, Color color)
		{
			SurfaceBegin(PrimitiveType.Lines);
			SurfaceSetColor(color);
			for (int i = 0; i < points.Length; ++i)
			{
				SurfaceAddVertex(points[i]);
			}
			SurfaceEnd();

			BaseMaterial3D material = new StandardMaterial3D
			{
				UsePointSize = true,
				PointSize = 5f,
				VertexColorUseAsAlbedo = true
			};
			SurfaceSetMaterial(GetSurfaceCount() - 1, material);
		}

		public void ClearLines()
		{
			ClearSurfaces();
		}
	}
}
