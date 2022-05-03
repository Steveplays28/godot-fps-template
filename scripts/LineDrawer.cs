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

		public void ClearLines()
		{
			Clear();
		}
	}
}
