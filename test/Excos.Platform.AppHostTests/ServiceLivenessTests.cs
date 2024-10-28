namespace Excos.Platform.AppHostTests.Tests;

public class ServiceLivenessTests
{
	[Fact]
	public async Task GetWebResourceRootReturnsOkStatusCode()
	{
		// Arrange
		await using var app = await AppHost.StartAsync();

		// Act
		var httpClient = await app.GetWebApiClientAsync();
		var response = await httpClient.GetAsync("/alive");

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}
}