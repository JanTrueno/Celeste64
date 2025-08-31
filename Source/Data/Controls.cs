
namespace Celeste64;

public class Controls : VirtualDevice
{
	public readonly VirtualStick Move;
	public readonly VirtualStick Camera;
	public readonly VirtualAction Jump;
	public readonly VirtualAction Dash;
	public readonly VirtualAction Climb;
	public readonly VirtualAction Pause;
	public readonly VirtualAction Confirm;
	public readonly VirtualAction Cancel;
	public readonly (VirtualAction Left, VirtualAction Right, VirtualAction Up, VirtualAction Down) Menu;

	private readonly Dictionary<string, Dictionary<string, string>> prompts = [];

	public Controls(Input input, ControlsConfig config) : base(input, "Controls")
	{
		IndexMode = IndexModes.AutomaticLatest;

		Move = AddStick("Move", config.Move);
		Camera = AddStick("Camera", config.Camera);
		Jump = AddAction("Jump", config.Jump);
		Dash = AddAction("Dash", config.Dash);
		Climb = AddAction("Climb", config.Climb);
		Pause = AddAction("Pause", config.Pause);
		Confirm = AddAction("Confirm", config.Confirm);
		Cancel = AddAction("Cancel", config.Cancel);
		Menu = (
			AddAction("MenuLeft", config.MenuLeft),
			AddAction("MenuRight", config.MenuRight),
			AddAction("MenuUp", config.MenuUp),
			AddAction("MenuDown", config.MenuDown)
		);
	}

	public void Consume()
	{
		Jump.ConsumePress();
		Dash.ConsumePress();
		Climb.ConsumePress();
		Confirm.ConsumePress();
		Cancel.ConsumePress();
		Pause.ConsumePress();
	}

	private static string GetControllerName(GamepadProviders pad) => pad switch
	{
		GamepadProviders.PlayStation => "PlayStation 5",
		GamepadProviders.Nintendo => "Nintendo Switch",
		GamepadProviders.Xbox => "Xbox Series",
		_ => "Xbox Series",
	};

	private string GetPromptLocation(string name)
	{
		var deviceTypeName = IsGamepadLatest
			? GetControllerName(Input.Controllers[ControllerIndex].GamepadProvider)
			: "PC";

		if (!prompts.TryGetValue(deviceTypeName, out var list))
			prompts[deviceTypeName] = list = [];

		if (!list.TryGetValue(name, out var lookup))
			list[name] = lookup = $"Controls/{deviceTypeName}/{name}";
					
		return lookup;
	}

	public string GetPromptLocation(VirtualAction button)
	{
		// TODO: instead, query the button's actual bindings and look up a
		// texture based on that! no time tho
		if (button == Confirm)
			return GetPromptLocation("confirm");
		else
			return GetPromptLocation("cancel");
	}

	public Subtexture GetPrompt(VirtualAction button)
	{
		return Assets.Subtextures.GetValueOrDefault(GetPromptLocation(button));
	}

	public bool IsUsingNintendo => Input.Controllers[ControllerIndex].GamepadProvider == GamepadProviders.Nintendo;
}
