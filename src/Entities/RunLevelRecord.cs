using System;
using System.Collections.Generic;

namespace AuthorTimeHunting.Entities;

/// <summary>
///     One level inside a stored run. Flat and primitive for the same reason as
///     <see cref="RunRecord" />: it has to stay readable long after the code that wrote it.
/// </summary>
public class RunLevelRecord
{
	public string Uid { get; set; }
	public string Name { get; set; }
	public string Author { get; set; }

	public string Status { get; set; }

	public int Attempts { get; set; }
	public int Crashes { get; set; }
	public int WheelsLost { get; set; }

	public double DurationMs { get; set; }

	public double AuthorTime { get; set; }
	public double GoldTime { get; set; }

	public double PersonalBest { get; set; }
}
