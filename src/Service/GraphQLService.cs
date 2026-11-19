using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

namespace AuthorTimeHunting.Service;

/// <summary>Queries the zeepki.st GraphQL backend for levels.</summary>
public class GraphQLService
{
	private GraphQLHttpClient _graphQLClient;

	public GraphQLService()
	{
		InitializeGraphQLClient();
	}

	/// <summary>
	///     Runs the cheapest query the schema allows. Returns null when the endpoint answered
	///     it, and the reason it did not otherwise.
	/// </summary>
	public async Task<string> PingAsync()
	{
		GraphQLRequest query = new() { Query = "query Ping { __typename }" };

		GraphQLResponse<Root> response = await _graphQLClient.SendQueryAsync<Root>(query).ConfigureAwait(false);

		if (response.Errors == null || !response.Errors.Any())
		{
			return null;
		}

		return string.Join(", ", response.Errors.Select(e => e.Message));
	}

	public async Task<List<LevelItem>> GetRandomLevelAsync(int amount = 100, int maxAuthorTime = 180)
	{
		try
		{
			Logger.LogInfo($"Fetching {amount} random levels with max author time of {maxAuthorTime} seconds.");
			GraphQLRequest query = new()
			{
				Query = $$$"""
				           query GetRandomLevel {
				             zRtm(
				               pMaxAuthorTime: {{{maxAuthorTime}}}
				               filter: {deleted: {equalTo: false}, amountFinishes: {notEqualTo: 0}}
				               first: {{{amount}}}
				             ) {
				               nodes {
				                 name
				                 validationTimeAuthor
				                 validationTimeGold
				                 fileAuthor
				                 fileUid
				                 workshopId
				                 authorId
				               }
				             }
				           }
				           """
			};
			GraphQLResponse<Root> response = await _graphQLClient.SendQueryAsync<Root>(query).ConfigureAwait(false);
			Logger.LogInfo($"Fetched {amount} random levels with max author time of {maxAuthorTime} seconds.");

			if (response.Errors != null && response.Errors.Any())
			{
				Logger.LogError(
					$"GraphQL response contains errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
				return null;
			}

			if (response.Data?.ZRtm?.Nodes == null)
			{
				Logger.LogError("Invalid response structure: ZRtm or Nodes is null");
				return null;
			}

			if (response.Data.ZRtm.Nodes.Count == 0)
			{
				Logger.LogError("No level data found in response");
				return null;
			}

			List<LevelItem> levelItems = new();

			foreach (Node node in response.Data.ZRtm.Nodes)
			{
				if (node == null)
				{
					continue;
				}

				levelItems.Add(new LevelItem
				{
					Name = node.Name,
					ValidationTimeAuthor = node.ValidationTimeAuthor,
					ValidationTimeGold = node.ValidationTimeGold,
					FileAuthor = node.FileAuthor,
					FileUid = node.FileUid,
					AuthorId = ParseId(node.AuthorId),
					WorkshopId = ParseId(node.WorkshopId)
				});
			}

			Logger.LogInfo($"Successfully retrieved {levelItems.Count} random levels");
			return levelItems;
		}
		catch (Exception ex)
		{
			Logger.LogError($"GraphQL GetRandomLevelAsync failed: {ex.Message}");
			throw;
		}
	}

	private static ulong ParseId(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return 0;
		}

		ulong.TryParse(id, out ulong parsed);
		return parsed;
	}

	private void InitializeGraphQLClient()
	{
		string graphQlUrl = Plugin.Instance.MyConfig.GraphQlUrl.Value;
		Logger.LogInfo($"Initializing GraphQL client at '{graphQlUrl}'");

		if (_graphQLClient != null)
		{
			return;
		}

		_graphQLClient = new GraphQLHttpClient(graphQlUrl, new NewtonsoftJsonSerializer());
		Logger.LogInfo("GraphQL client initialized.");
	}
}
