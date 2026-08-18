
namespace Celeste64;

public struct RenderState
{
	public Camera Camera;
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
		if (mat.Shader == null)
			return;

		mat.Model = localTransformation * ModelMatrix;
		mat.MVP = mat.Model * Camera.ViewProjection;
		mat.NearPlane = Camera.NearPlane;
		mat.FarPlane = Camera.FarPlane;
		mat.Silhouette = Silhouette;
		mat.Time = (float)Time.Duration.TotalSeconds;
		mat.SunDirection = SunDirection;
		mat.VerticalFogColor = VerticalFogColor;
		mat.Cutout = CutoutMode;
	}

	/// <summary>
	/// Applies shared values to a material used for instanced drawing.
	/// The instance matrix is provided per-instance, so the Model matrix is identity
	/// and the MVP only contains the Camera's view projection.
	/// </summary>
	public void ApplyToInstancedMaterial(DefaultMaterial mat, DefaultMaterial source)
	{
		if (mat.Shader == null)
			return;

		mat.Texture = source.Texture;
		mat.Color = source.Color;
		mat.Model = Matrix.Identity;
		mat.MVP = Camera.ViewProjection;
		mat.NearPlane = Camera.NearPlane;
		mat.FarPlane = Camera.FarPlane;
		mat.Silhouette = Silhouette;
		mat.SilhouetteColor = source.SilhouetteColor;
		mat.Time = (float)Time.Duration.TotalSeconds;
		mat.SunDirection = SunDirection;
		mat.VerticalFogColor = VerticalFogColor;
		mat.Cutout = CutoutMode;
	}
}