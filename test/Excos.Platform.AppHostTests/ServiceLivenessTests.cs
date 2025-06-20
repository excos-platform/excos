// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

namespace Excos.Platform.AppHostTests.Tests;

public class ServiceLivenessTests
{
	[Fact]
	public async Task GetWebResourceRootReturnsOkStatusCode()
	{
		// Arrange
		await using FastTestDistributedApplication app = await AppHost.StartAsync();

		// Act
		HttpClient httpClient = await app.GetWebApiClientAsync();
		HttpResponseMessage response = await httpClient.GetAsync("/alive");

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}
}