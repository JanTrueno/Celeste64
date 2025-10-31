using System.Runtime.InteropServices;

namespace Celeste64;

public class SpriteRenderer
{
	private record struct SpriteBatch(Texture Texture, int Index, int Count);
	private readonly List<SpriteVertex> spriteVertices = [];
	private readonly List<int> spriteIndices = [];
	private readonly List<SpriteBatch> spriteBatches = [];
	private readonly Mesh<SpriteVertex> spriteMesh = new(Game.Instance.GraphicsDevice);
	private readonly Material spriteMaterial = new(Assets.Materials["Sprite"]);

	public void Render(ref RenderState state, List<Sprite> sprites, bool postEffects)
	{
		spriteVertices.Clear();
		spriteIndices.Clear();
		spriteBatches.Clear();

		SpriteBatch current = new();

		foreach (var board in sprites)
		{
			if (board.Subtexture.Texture == null)
				continue;

			if (board.Post != postEffects)
				continue;

			int i = spriteVertices.Count;

			if (board.Subtexture.Texture != current.Texture)
			{
				if (current.Count > 0)
					spriteBatches.Add(current);
				current.Texture = board.Subtexture.Texture;
				current.Index = i;
				current.Count = 0;
			}

			spriteVertices.Add(new(board.A, board.Subtexture.TexCoords[0], board.Color));
			spriteVertices.Add(new(board.B, board.Subtexture.TexCoords[1], board.Color));
			spriteVertices.Add(new(board.C, board.Subtexture.TexCoords[2], board.Color));
			spriteVertices.Add(new(board.D, board.Subtexture.TexCoords[3], board.Color));

			spriteIndices.Add(i + 0);
			spriteIndices.Add(i + 1);
			spriteIndices.Add(i + 2);
			spriteIndices.Add(i + 0);
			spriteIndices.Add(i + 2);
			spriteIndices.Add(i + 3);

			current.Count += 6;
		}

		if (current.Count > 0)
			spriteBatches.Add(current);

		spriteMesh.SetVertices(CollectionsMarshal.AsSpan(spriteVertices));
		spriteMesh.SetIndices(CollectionsMarshal.AsSpan(spriteIndices));

		spriteMaterial.Vertex.SetUniformBuffer(new UniformBuffers.SpriteVertex(state.Camera.ViewProjection));
		spriteMaterial.Fragment.SetUniformBuffer(new UniformBuffers.SpriteFragment(state.Camera));

		foreach (var batch in spriteBatches)
		{
			spriteMaterial.Fragment.Samplers[0] = new(batch.Texture, new TextureSampler(TextureFilter.Linear, TextureWrap.Clamp, TextureWrap.Clamp));

			state.GraphicsDevice.Draw(new DrawCommand(state.Camera.Target, spriteMesh, spriteMaterial)
			{
				BlendMode = BlendMode.Premultiply,
				DepthTestEnabled = true,
				DepthWriteEnabled = false,
				DepthCompare = postEffects ? DepthCompare.Always : DepthCompare.Less,
				CullMode = CullMode.None,
				IndexOffset = batch.Index,
				IndexCount = batch.Count
			});
			state.Calls++;
			state.Triangles += batch.Count / 3;
		}
	}
}