using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthorTimeHunting.Util;

/// <summary>
///     Waits for a game state the game does not announce. Polls, so it cannot miss an edge
///     that fired while nothing was subscribed.
/// </summary>
public static class Wait
{
	private static readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(250);

	/// <summary>Returns false when the timeout ran out first.</summary>
	public static async Task<bool> UntilAsync(Func<bool> ready, TimeSpan timeout, CancellationToken token)
	{
		DateTime deadline = DateTime.UtcNow + timeout;

		while (!ready() && DateTime.UtcNow < deadline)
		{
			await Task.Delay(_pollInterval, token);
		}

		return ready();
	}
}
