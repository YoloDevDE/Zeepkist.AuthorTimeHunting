using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthorTimeHunting.Gamemodes;

/// <summary>
///     Every mode the mod knows, and which one the next run will use. Deliberately not persisted.
/// </summary>
public class GamemodeRegistry
{
	private readonly List<IGamemode> _modes =
	[
		new ClassicGamemode()
	];

	public GamemodeRegistry()
	{
		Selected = _modes[0];
		DisplayNames = _modes.Select(mode => mode.DisplayName).ToArray();
	}

	public IReadOnlyList<IGamemode> All => _modes;

	/// <summary>What a dropdown lists, in the order of <see cref="All" />. The list never changes.</summary>
	public string[] DisplayNames { get; }

	public IGamemode Selected { get; set; }

	public IGamemode Resolve(string id)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}

		return _modes.FirstOrDefault(mode => string.Equals(mode.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
	}
}
