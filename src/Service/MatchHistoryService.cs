using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using BepInEx;
using Newtonsoft.Json;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Every finished run on this machine, kept in one JSON file next to the mod's config.
///     Loaded once and held in memory, so no disk IO happens inside a GUI pass.
/// </summary>
public class MatchHistoryService
{
	private const string _fileName = "AuthorTimeHunting.history.json";

	private const int _maxRecords = 200;

	private List<RunRecord> _records;

	public IReadOnlyList<RunRecord> Records => _records ??= Load();

	private static string Path => System.IO.Path.Combine(Paths.ConfigPath, _fileName);

	public void Add(RunRecord record)
	{
		if (record == null)
		{
			return;
		}

		List<RunRecord> records = _records ??= Load();

		records.Insert(0, record);

		if (records.Count > _maxRecords)
		{
			records.RemoveRange(_maxRecords, records.Count - _maxRecords);
		}

		Save(records);
	}

	private static List<RunRecord> Load()
	{
		try
		{
			if (!File.Exists(Path))
			{
				return [];
			}

			List<RunRecord> records = JsonConvert.DeserializeObject<List<RunRecord>>(File.ReadAllText(Path));

			if (records == null)
			{
				return [];
			}

			return records.Where(record => record != null).OrderByDescending(record => record.EndedAt).ToList();
		}
		catch (Exception e)
		{
			Logger.LogError($"MatchHistoryService: Could not read {Path}: {e.Message}");
			return [];
		}
	}

	private static void Save(List<RunRecord> records)
	{
		try
		{
			File.WriteAllText(Path, JsonConvert.SerializeObject(records, Formatting.Indented));
		}
		catch (Exception e)
		{
			Logger.LogError($"MatchHistoryService: Could not write {Path}: {e.Message}");
		}
	}
}
