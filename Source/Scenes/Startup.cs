using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Celeste64;

/// <summary>
/// Creates a slight delay so the window looks OK before we load Assets
/// TODO: Would be nice if Foster could hide the Window till assets are ready.
/// </summary>
public class Startup : Scene
{
	private int loadDelay = 5;

	private void BeginGame()
	{
		// load assets
		Assets.Load(GraphicsDevice);

		// load save file
		{
			var saveFile = Path.Join(Game.UserPath, Save.FileName);

			if (File.Exists(saveFile))
				Save.Instance = Save.Deserialize(File.ReadAllText(saveFile)) ?? new();
			else
				Save.Instance = new();
			Save.Instance.ApplySettings(Game);
		}

		// make sure the active language is ready for use,
		// since the save file may have loaded a different language than default.
		Language.Current.Use(GraphicsDevice);

		// try to load controls, or overwrite with defaults if they don't exist
		// if (false)
		{
			var controlsFile = Path.Join(Game.UserPath, ControlsConfig.FileName);

			ControlsConfig? controlConfig = null;
			if (File.Exists(controlsFile))
			{
				try
				{
					controlConfig = JsonSerializer.Deserialize(File.ReadAllText(controlsFile), ControlsConfigContext.Default.ControlsConfig);
				}
				catch
				{
					controlConfig = null;
				}
			}

			// create defaults if not found
			if (controlConfig == null)
			{
				controlConfig = new();
				
				var data = JsonSerializer.Serialize(controlConfig, ControlsConfigContext.Default.ControlsConfig);
				data = MakeBindingsOneLine(data);
				File.WriteAllText(controlsFile, data);
			}
			else
			{
				Game.Controls = new(Input, controlConfig);
			}
		}

		// enter game
		//Assets.Levels[0].Enter(new AngledWipe());
		Game.Goto(new Transition()
		{
			Mode = Transition.Modes.Replace,
			Scene = () => new Titlescreen(),
			ToBlack = null,
			FromBlack = new AngledWipe(),
		});
	}

    public override void Update()
    {
		if (loadDelay > 0)
		{
			loadDelay--;
			if (loadDelay <= 0)
				BeginGame();
		}
    }

    public override void Render(Target target)
    {
		target.Clear(Color.Black);
    }

	/// <summary>
	/// This is all so that each Binding in the Controls Json file ends up on one line.
	/// Otherwise the Json file is huge
	/// </summary>
	private static string MakeBindingsOneLine(string input)
	{
		StringBuilder result = new();

		var depth = 0;
		var skippingWhitespace = false;

		for (int i = 0; i < input.Length; i ++)
		{
			if (!skippingWhitespace && input[i] == '{')
			{
				int n = 1;
				while (char.IsWhiteSpace(input[i + n]))
					n++;
				if (input.AsSpan(i + n).StartsWith("\"$type\":"))
					skippingWhitespace = true;
			}

			if (skippingWhitespace)
			{
				if (input[i] == '{')
					depth++;
				if (input[i] == '}')
				{
					depth--;
					if (depth <= 0)
						skippingWhitespace = false;
				}
			}

			if (!char.IsWhiteSpace(input[i]) || !skippingWhitespace)
				result.Append(input[i]);
		}

		return result.ToString();
	}
}