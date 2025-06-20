using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting;
using LimeFlight.OpenAPI.Diff;
using LimeFlight.OpenAPI.Diff.BusinessObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using MyProject;

namespace Excos.Platform.AppHostTests;

public static class OpenApiSnapshot
{
	private static string OpenApiSnapshotPath(string version) => ProjectInfo.Directory + $"\\Excos.OpenApi.Snapshot.{version}.json";

	private static async Task<string> FetchOpenApiFileAsync(string version)
	{
		await using FastTestDistributedApplication app = await AppHost.StartAsync();
		HttpClient client = await app.GetWebApiClientAsync();

		HttpResponseMessage response = await client.GetAsync($"/swagger/{version}/swagger.json");
		if (response.StatusCode != HttpStatusCode.OK)
		{
			throw new Exception($"Failed to fetch OpenAPI spec. Status code: {response.StatusCode}");
		}

		return await response.Content.ReadAsStringAsync();
	}

	private static OpenApiDocument ParseOpenApiSpec(string content)
	{
		var reader = new OpenApiStringReader();
		var diagnostic = new OpenApiDiagnostic();
		return reader.Read(content, out diagnostic);
	}

	[Theory]
	[InlineData("V1")]
	public static async Task OpenApiSpecIsBackwardsCompatible(string version)
	{
		string currentOpenApi = await FetchOpenApiFileAsync(version);

		if (!File.Exists(OpenApiSnapshotPath(version)))
		{
			await File.WriteAllTextAsync(OpenApiSnapshotPath(version), currentOpenApi);
		}
		else
		{
			string previousOpenApi = await File.ReadAllTextAsync(OpenApiSnapshotPath(version));

			ChangedOpenApiBO comparison = new OpenAPICompare(new NullLogger<OpenAPICompare>(), [])
				.FromSpecifications(
					ParseOpenApiSpec(previousOpenApi), "previous",
					ParseOpenApiSpec(currentOpenApi), "current");

			IEnumerable<ChangedInfoBO> incompatibleChanges = comparison
				.GetAllChangeInfoFlat(null)
				.Where(change => change.ChangeType.IsIncompatible())
				.SelectMany(change => change.Changes);

			Assert.True(
				comparison.IsCompatible(),
				"OpenAPI spec should be backwards compatible. Changes:\n" +
				string.Join("\n", incompatibleChanges.Select(x => JsonSerializer.Serialize(x, new JsonSerializerOptions
				{
					Converters =
					{
						new JsonStringEnumConverter()
					}
				})).Distinct())
			);

			// Write the current OpenAPI spec to the file for future comparisons
			await File.WriteAllTextAsync(OpenApiSnapshotPath(version), currentOpenApi);
		}
	}
}