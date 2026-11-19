using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Asks the two backends a hunt depends on whether they are still there, both at once.
///     GraphQL gets a real query; GTR only a plain GET, where any answer counts as up.
/// </summary>
public class HealthService : IDisposable
{
	private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(8);

	private readonly GraphQLService _graphQL;

	private readonly HttpClient _http = new() { Timeout = _requestTimeout };

	public HealthService(GraphQLService graphQL)
	{
		_graphQL = graphQL;
	}

	public HealthStatus GraphQl { get; } = new("GraphQL");

	public HealthStatus Gtr { get; } = new("GTR backend");

	public bool IsChecking { get; private set; }

	public void Dispose()
	{
		_http.Dispose();
	}

	public void Refresh()
	{
		if (IsChecking)
		{
			return;
		}

		_ = RefreshAsync();
	}

	private async Task RefreshAsync()
	{
		IsChecking = true;

		try
		{
			await Task.WhenAll(CheckGraphQlAsync(), CheckGtrAsync());
		}
		finally
		{
			IsChecking = false;
		}
	}

	private async Task CheckGraphQlAsync()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();

		try
		{
			string error = await _graphQL.PingAsync();
			stopwatch.Stop();

			GraphQl.Report(error == null, error ?? "answered a query", stopwatch.ElapsedMilliseconds);
		}
		catch (Exception e)
		{
			stopwatch.Stop();
			Logger.LogWarning($"HealthService: GraphQL check failed: {e.Message}");
			GraphQl.Report(false, Describe(e), stopwatch.ElapsedMilliseconds);
		}
	}

	private async Task CheckGtrAsync()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();

		try
		{
			HttpResponseMessage response = await _http.GetAsync(Plugin.Instance.MyConfig.GtrUrl.Value);
			stopwatch.Stop();

			GtrUp(response, stopwatch.ElapsedMilliseconds);
		}
		catch (Exception e)
		{
			stopwatch.Stop();
			Logger.LogWarning($"HealthService: GTR check failed: {e.Message}");
			Gtr.Report(false, Describe(e), stopwatch.ElapsedMilliseconds);
		}
	}

	private void GtrUp(HttpResponseMessage response, long latencyMs)
	{
		Gtr.Report(true, $"answered {(int)response.StatusCode}", latencyMs);
		response.Dispose();
	}

	private static string Describe(Exception e)
	{
		if (e is TaskCanceledException)
		{
			return $"no answer within {_requestTimeout.TotalSeconds:F0}s";
		}

		return e.InnerException?.Message ?? e.Message;
	}
}
