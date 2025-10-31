using System.Diagnostics;

namespace Celeste64;

public class DefaultMaterial : Material
{
	public string Name = string.Empty;

    public Texture? Texture
    {
        get => Fragment.Samplers[0].Texture;
        set => Fragment.Samplers[0] = new(value, new(TextureFilter.Nearest, TextureWrap.Repeat, TextureWrap.Repeat));
    }

    public UniformBuffers.DefaultFragment FragmentUniforms
    {
        get => Fragment.GetUniformBuffer<UniformBuffers.DefaultFragment>();
        set => Fragment.SetUniformBuffer(value);
    }

    public UniformBuffers.DefaultVertex VertexUniforms
    {
        get => Vertex.GetUniformBuffer<UniformBuffers.DefaultVertex>();
        set => Vertex.SetUniformBuffer(value);
    }

    public Color Color
    {
        get => FragmentUniforms.Color;
        set => FragmentUniforms = FragmentUniforms with { Color = value };
    }

    public Color SilhouetteColor
    {
        get => FragmentUniforms.SilhouetteColor;
        set => FragmentUniforms = FragmentUniforms with { SilhouetteColor = value };
    }

    public float Effects
    {
        get => FragmentUniforms.Effects;
        set => FragmentUniforms = FragmentUniforms with { Effects = value };
    }

    public DefaultMaterial(Texture? texture = null)
		: base(Assets.Materials["Default"])
	{
        Texture = texture;
        FragmentUniforms = new()
        {
            Color = Color.White,
            Effects = 1.0f
        };
    }

    public new virtual DefaultMaterial Clone()
	{
		var copy = new DefaultMaterial(Texture);
		CopyTo(copy);
        return copy;
	}
}