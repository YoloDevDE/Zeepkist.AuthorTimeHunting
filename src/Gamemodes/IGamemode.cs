using System.Collections.Generic;

namespace AuthorTimeHunting.Gamemodes;

/// <summary>
///     One way of playing ATH: an identity plus the rules a run starts with. Adding a mode is one
///     file implementing this and one line in <see cref="GamemodeRegistry" />.
/// </summary>
public interface IGamemode
{
	string Id { get; }

	string DisplayName { get; }

	string Description { get; }

	IReadOnlyList<string> Rules { get; }

	RunSettings CreateSettings();
}
