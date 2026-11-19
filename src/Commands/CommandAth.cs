using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

/// <summary>
///     The mod's front door: /ath shows or hides the ATH window, exactly like the top bar's ATH entry.
/// </summary>
public class CommandAth : ILocalChatCommand
{
	public static event Action CommandTrigger;

	public string Prefix => "/";
	public string Command => "ath";

	public string Description => "Shows or hides the Author-Time-Hunting window.";

	public void Handle(string arguments)
	{
		if (!string.IsNullOrWhiteSpace(arguments))
		{
			return;
		}

		Raise();
	}

	public static void Raise()
	{
		CommandTrigger?.Invoke();
	}
}
