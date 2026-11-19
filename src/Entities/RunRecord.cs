using System;
using System.Collections.Generic;

namespace AuthorTimeHunting.Entities;

/// <summary>
///     One finished run, as it is written to disk. Flat and primitive on purpose, so an old
///     record stays readable whatever the rest of the mod becomes.
/// </summary>
public class RunRecord
{
	public DateTime EndedAt { get; set; }

	public string PlayerName { get; set; }

	public string Gamemode { get; set; }

	public int LevelsPlayed { get; set; }
	public int AuthorMedals { get; set; }
	public int GoldMedals { get; set; }
	public int Penalties { get; set; }
	public int TotalAttempts { get; set; }

	public double DurationMs { get; set; }

	public double DrivenMs { get; set; }

	public bool RanOutOfTime { get; set; }

	public List<RunLevelRecord> Levels { get; set; }
}
