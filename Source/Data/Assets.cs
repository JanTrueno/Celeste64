using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celeste64;

public static partial class Assets
{
	public const float FontSize = Game.RelativeScale * 16;
	public const string AssetFolder = "Content";

	private static string? contentPath = null;

	public static string ContentPath
	{
		get
		{
			if (contentPath == null)
			{
				var baseFolder = AppContext.BaseDirectory;
				var searchUpPath = "";
				int up = 0;
				while (!Directory.Exists(Path.Join(baseFolder, searchUpPath, AssetFolder)) && up++ < 5)
					searchUpPath = Path.Join(searchUpPath, "..");
				if (!Directory.Exists(Path.Join(baseFolder, searchUpPath, AssetFolder)))
					throw new Exception($"Unable to find {AssetFolder} Directory from '{baseFolder}'");
				contentPath = Path.Join(baseFolder, searchUpPath, AssetFolder);
			}

			return contentPath;
		}
	}

	public static readonly Dictionary<string, Map> Maps = new(StringComparer.OrdinalIgnoreCase);
	public static readonly Dictionary<string, Material> Materials = new(StringComparer.OrdinalIgnoreCase);
	public static readonly Dictionary<string, Texture> Textures = new(StringComparer.OrdinalIgnoreCase);
	public static readonly Dictionary<string, SkinnedTemplate> Models = new(StringComparer.OrdinalIgnoreCase);
	public static readonly Dictionary<string, Subtexture> Subtextures = new(StringComparer.OrdinalIgnoreCase);
	public static readonly Dictionary<string, Font> Fonts = new(StringComparer.OrdinalIgnoreCase);
	public static readonly Dictionary<string, Language> Languages = new(StringComparer.OrdinalIgnoreCase);
	public static List<LevelInfo> Levels { get; private set; } = [];

	public static void Load(GraphicsDevice gfx)
	{
		var timer = Stopwatch.StartNew();

		Levels.Clear();
		Maps.Clear();
		Materials.Clear();
		Textures.Clear();
		Subtextures.Clear();
		Models.Clear();
		Fonts.Clear();
		Languages.Clear();
		Audio.Unload();

		var maps = new ConcurrentBag<Map>();
		var images = new ConcurrentBag<(string, Image)>();
		var models = new ConcurrentBag<(string, SkinnedTemplate)>();
		var langs = new ConcurrentBag<Language>();
		var tasks = new List<Task>();

		// load map files
		{
			var mapsPath = Path.Join(ContentPath, "Maps");
			foreach (var file in Directory.EnumerateFiles(mapsPath, "*.map", SearchOption.AllDirectories))
			{
				var name = GetResourceName(mapsPath, file);
				if (name.StartsWith("autosave", StringComparison.OrdinalIgnoreCase))
					continue;

				tasks.Add(Task.Run(() =>
				{
					var map = new Map(name, file);
					maps.Add(map);
				}));
			}
		}

		// load texture pngs
		var texturesPath = Path.Join(ContentPath, "Textures");
		foreach (var file in Directory.EnumerateFiles(texturesPath, "*.png", SearchOption.AllDirectories))
		{
			var name = GetResourceName(texturesPath, file);
			tasks.Add(Task.Run(() =>
			{
				var img = new Image(file);
				img.Premultiply();
				images.Add((name, img));
			}));
		}

		// load faces
		var facesPath = Path.Join(ContentPath, "Faces");
		foreach (var file in Directory.EnumerateFiles(facesPath, "*.png", SearchOption.AllDirectories))
		{
			var name = $"faces/{GetResourceName(facesPath, file)}";
			tasks.Add(Task.Run(() =>
			{
				var img = new Image(file);
				img.Premultiply();
				images.Add((name, img));
			}));
		}

		// load glb models
		var modelPath = Path.Join(ContentPath, "Models");
		foreach (var file in Directory.EnumerateFiles(modelPath, "*.glb", SearchOption.AllDirectories))
		{
			var name = GetResourceName(modelPath, file);

			tasks.Add(Task.Run(() =>
			{
				var input = SharpGLTF.Schema2.ModelRoot.Load(file);
				var model = new SkinnedTemplate(input);
				models.Add((name, model));
			}));
		}

		// load languages
		var textPath = Path.Join(ContentPath, "Text");
		foreach (var file in Directory.EnumerateFiles(textPath, "*.json", SearchOption.AllDirectories))
		{
			tasks.Add(Task.Run(() =>
			{
				var data = File.ReadAllText(file);
				if (JsonSerializer.Deserialize(data, LanguageContext.Default.Language) is {} lang)
					langs.Add(lang);
			}));
		}

		// load audio
		Audio.Load(Path.Join(ContentPath, "Audio"));

		// load level json
		{
			var data = File.ReadAllText(Path.Join(ContentPath, "Levels.json"));
			Levels = JsonSerializer.Deserialize(data, LevelInfoListContext.Default.ListLevelInfo) ?? [];
		}

		// load glsl shaders
		var shadersPath = Path.Join(ContentPath, "Shaders");
		foreach (var file in Directory.EnumerateFiles(shadersPath, "*.hlsl"))
		{
			var name = GetResourceName(shadersPath, file);
			if (LoadShader(gfx, name) is Material material)
				Materials[name] = material;
		}

		// load font files
		var fontsPath = Path.Join(ContentPath, "Fonts");
		foreach (var file in Directory.EnumerateFiles(fontsPath, "*.*", SearchOption.AllDirectories))
			if (file.EndsWith(".ttf") || file.EndsWith(".otf"))
				Fonts.Add(GetResourceName(fontsPath, file), new Font(file));

		// pack sprites into single texture
		{
			var packer = new Packer
			{
				Trim = false,
				CombineDuplicates = false,
				Padding = 1
			};

			var spritesPath = Path.Join(ContentPath, "Sprites");
			foreach (var file in Directory.EnumerateFiles(spritesPath, "*.png", SearchOption.AllDirectories))
				packer.Add(GetResourceName(spritesPath, file), new Image(file));

			var result = packer.Pack();
			var pages = new List<Texture>();
			foreach (var it in result.Pages)
			{
				it.Premultiply();
				pages.Add(new Texture(gfx, it));
			}

			foreach (var it in result.Entries)
				Subtextures.Add(it.Name, new Subtexture(pages[it.Page], it.Source, it.Frame));
		}

		// wait for tasks to finish
		{
			foreach (var task in tasks)
				task.Wait();
			foreach (var (name, img) in images)
				Textures.Add(name, new Texture(gfx, img, name: name));
			foreach (var map in maps)
				Maps[map.Name] = map;
			foreach (var (name, model) in models)
			{
				model.ConstructResources(gfx);
				Models[name] = model;
			}
			foreach (var lang in langs)
			{
				if (Languages.TryGetValue(lang.ID, out var existing))
					existing.Absorb(lang);
				else
					Languages[lang.ID] = lang;
			}
		}

		// make sure the active language is ready for use
		Language.Current.Use(gfx);

		Log.Info($"Loaded Assets in {timer.ElapsedMilliseconds}ms");
	}

	private static string GetResourceName(string contentFolder, string path)
	{
		var fullname = Path.Join(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
		var relative = Path.GetRelativePath(contentFolder, fullname);
		var normalized = relative.Replace("\\", "/");
		return normalized;
	}

	internal partial struct ShaderReflection
	{
		[JsonInclude] public int samplers;
		[JsonInclude] public int storage_textures;
		[JsonInclude] public int storage_buffers;
		[JsonInclude] public int uniform_buffers;
	}

	private static Material? LoadShader(GraphicsDevice gfx, string name)
	{
		static ShaderReflection GetReflection(string file)
		{
			var json = File.ReadAllText(file);
			return JsonSerializer.Deserialize(json, AssetShaderReflectionContext.Default.ShaderReflection);
		}

		var path = Path.Combine(ContentPath, "Shaders", "Compiled", name);
		var vMeta = GetReflection(path + ".vertex.json");
		var fMeta = GetReflection(path + ".fragment.json");
		var ext = gfx.Driver.GetShaderExtension();
		var vertexShader = new Shader(gfx,
			stage: ShaderStage.Vertex,
			code: File.ReadAllBytes(path + ".vertex." + ext),
			samplerCount: vMeta.samplers,
			uniformBufferCount: vMeta.uniform_buffers,
			entryPoint: "vertex_main",
			name: $"{name}.Vertex"
		);
		var fragmentShader = new Shader(gfx,
			stage: ShaderStage.Fragment,
			code: File.ReadAllBytes(path + ".fragment." + ext),
			samplerCount: fMeta.samplers,
			uniformBufferCount: fMeta.uniform_buffers,
			entryPoint: "fragment_main",
			name: $"{name}.Vertex"
		);

		return new Material(vertexShader, fragmentShader);
	}
}

[JsonSourceGenerationOptions(WriteIndented = true, AllowTrailingCommas = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Assets.ShaderReflection))]
internal partial class AssetShaderReflectionContext : JsonSerializerContext {}