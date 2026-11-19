using System;

namespace AuthorTimeHunting.Commands;

/// <summary>
///     What the UI asks the run to do: start, stop, restart, or write the level off as broken.
///     The master state machine listens, since only it knows whether a run exists.
/// </summary>
public static class AthRequests
{
	public static event Action StartRequested;

	public static event Action StopRequested;

	public static event Action RestartRequested;

	public static event Action SkipBrokenRequested;

	public static void Start()
	{
		StartRequested?.Invoke();
	}

	public static void Stop()
	{
		StopRequested?.Invoke();
	}

	public static void Restart()
	{
		RestartRequested?.Invoke();
	}

	public static void SkipBroken()
	{
		SkipBrokenRequested?.Invoke();
	}
}
