// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

namespace Excos.Platform.AppHostTests;

public static class AppHost
{
	public static async Task<DistributedApplication> StartAsync()
	{
		IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Excos_Platform_AppHost>();
		appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
		{
			clientBuilder.AddStandardResilienceHandler();
		});
		// To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging

		DistributedApplication app = await appHost.BuildAsync();
		await app.StartAsync();

		return app;
	}

	public static async Task<HttpClient> GetWebApiClientAsync(this DistributedApplication app)
	{
		ResourceNotificationService resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
		await resourceNotificationService.WaitForResourceAsync("WebApiHost", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));

		HttpClient httpClient = app.CreateHttpClient("WebApiHost");
		return httpClient;
	}
}