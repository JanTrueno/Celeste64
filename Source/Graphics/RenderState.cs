
namespace Celeste64;

public struct RenderState(GraphicsDevice gfx, in Time time)
{
	public readonly GraphicsDevice GraphicsDevice = gfx;
	public readonly Time Time = time;

	public Camera Camera = new();
	public Matrix ModelMatrix;
	public bool Silhouette;
	public Vec3 SunDirection;
	public Color VerticalFogColor;
	public DepthCompare DepthCompare;
	public bool DepthMask;
	public bool CutoutMode;
	public int Calls;
	public int Triangles;

	public void ApplyToMaterial(DefaultMaterial mat, in Matrix localTransformation)
	{
		if (mat.Vertex.Shader == null || mat.Fragment.Shader == null)
			return;

		var vertex = mat.VertexUniforms;
		vertex.Model = localTransformation * ModelMatrix;
		vertex.MVP = vertex.Model * Camera.ViewProjection;
		Matrix.Invert(vertex.Model, out vertex.ModelInverse);
		mat.VertexUniforms = vertex;

		var fragment = mat.FragmentUniforms;
		fragment.Near = Camera.NearPlane;
		fragment.Far = Camera.FarPlane;
		fragment.Silhouette = Silhouette ? 1 : 0;
		fragment.Time = (float)Time.Elapsed.TotalSeconds;
		fragment.Sun = new Vec4(SunDirection, 0);
		fragment.VerticalFogColor = VerticalFogColor;
		fragment.Cutout = CutoutMode ? 1 : 0;
		mat.FragmentUniforms = fragment;
	}
}