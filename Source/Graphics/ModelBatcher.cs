
using System.Runtime.InteropServices;

namespace Celeste64;

/// <summary>
/// Collects static model parts into instanced draw batches.
/// Cleared and Flushed once per render pass.
/// </summary>
public class ModelBatcher
{
	private readonly record struct Key(
		Mesh Mesh, int IndexStart, int IndexCount, DefaultMaterial Material);

	private class Group
	{
		public Key Key;
		public readonly List<Matrix> Instances = [];
		public DefaultMaterial InstancedMaterial = null!;
	}

	// one mat4 per instance: 4x Float4 attributes at locations 6-9
	private static readonly VertexFormat InstanceFormat = new(
		64,
		new VertexFormat.Element(6, VertexType.Float4, false),
		new VertexFormat.Element(7, VertexType.Float4, false),
		new VertexFormat.Element(8, VertexType.Float4, false),
		new VertexFormat.Element(9, VertexType.Float4, false));

	private readonly Dictionary<Key, Group> groups = [];

	public void Clear()
	{
		foreach (var group in groups.Values)
			group.Instances.Clear();
	}

	public void Add(Matrix instance, Mesh mesh, int indexStart, int indexCount, DefaultMaterial material)
	{
		var key = new Key(mesh, indexStart, indexCount, material);

		if (!groups.TryGetValue(key, out var group))
		{
			group = new Group { Key = key };
			groups.Add(key, group);
		}

		group.Instances.Add(instance);
	}

	public void Flush(ref RenderState state)
	{
		foreach (var group in groups.Values)
		{
			int count = group.Instances.Count;
			if (count <= 0)
				continue;

			var key = group.Key;

			if (group.InstancedMaterial == null)
			{
				group.InstancedMaterial = new DefaultMaterial(Assets.Shaders["DefaultInstanced"]);
				if (group.InstancedMaterial.Shader?.Has("u_jointMult") ?? false)
					group.InstancedMaterial.Set("u_jointMult", 0.0f);
			}

			state.ApplyToInstancedMaterial(group.InstancedMaterial, key.Material);

			key.Mesh.SetInstances<Matrix>(CollectionsMarshal.AsSpan(group.Instances), InstanceFormat);

			DrawCommand cmd = new(state.Camera.Target, key.Mesh, group.InstancedMaterial)
			{
				DepthCompare = state.DepthCompare,
				DepthMask = state.DepthMask,
				CullMode = CullMode.Back,
				MeshIndexStart = key.IndexStart,
				MeshIndexCount = key.IndexCount,
				InstanceCount = count
			};
			cmd.Submit();

			state.Calls++;
			state.Triangles += (key.IndexCount / 3) * count;
		}
	}
}
