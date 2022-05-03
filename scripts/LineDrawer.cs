using Godot;

namespace SteveUtility
{
	public class LineDrawer : ImmediateGeometry
	{
		public void DrawLine(Vector3[] points)
		{
			Begin(Mesh.PrimitiveType.Lines);
			for (int i = 0; i < points.Length; ++i)
			{
				AddVertex(points[i]);
			}
			End();
		}
		public void DrawLine(Vector3[] points, Color color)
		{
			Begin(Mesh.PrimitiveType.Lines);
			var spatialMaterial = new SpatialMaterial
			{
				FlagsUsePointSize = true,
				ParamsPointSize = 5f,
				VertexColorUseAsAlbedo = true
			};
			MaterialOverride = spatialMaterial;
			SetColor(color);
			for (int i = 0; i < points.Length; ++i)
			{
				AddVertex(points[i]);
			}
			End();
		}

		public void ClearLines()
		{
			Clear();
		}
	}
}