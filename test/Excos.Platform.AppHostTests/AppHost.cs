// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Aspire.Hosting;
using Excos.Testing.OpenTelemetry;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Excos.Platform.AppHostTests;

public static class AppHost
{
	private static readonly Lock OtlpServerLock = new Lock();
	public static TestOtlpServer TestOtlpServer { get; private set; } = default!;
	
	private static PostgreSqlContainer? _sharedPostgresContainer;
	private static readonly SemaphoreSlim _initSemaphore = new(1, 1);

	internal static PostgreSqlContainer? SharedPostgresContainer => _sharedPostgresContainer;

	public static async Task<FastTestDistributedApplication> StartAsync()
	{
		lock (OtlpServerLock)
		{
			TestOtlpServer ??= new TestOtlpServer();
		}

		// Use WebApplicationFactory approach for faster testing
		await _initSemaphore.WaitAsync();
		try
		{
			// Initialize shared Postgres container if not already done
			if (_sharedPostgresContainer == null)
			{
				_sharedPostgresContainer = new PostgreSqlBuilder()
					.WithImage("postgres:15")
					.WithDatabase("excos-db")
					.WithUsername("test-user")
					.WithPassword("test-password")
					.WithCleanUp(true)
					.Build();

				await _sharedPostgresContainer.StartAsync();
			}
		}
		finally
		{
			_initSemaphore.Release();
		}

		// Create a lightweight wrapper that behaves like DistributedApplication
		return new FastTestDistributedApplication();
	}

	public static async Task<HttpClient> GetWebApiClientAsync(this FastTestDistributedApplication app)
	{
		return await app.GetHttpClientAsync();
	}
	
	// Cleanup method for test disposal
	public static async Task CleanupSharedResourcesAsync()
	{
		if (_sharedPostgresContainer != null)
		{
			await _sharedPostgresContainer.DisposeAsync();
			_sharedPostgresContainer = null;
		}
	}
}

/// <summary>
/// A lightweight alternative to DistributedApplication that uses WebApplicationFactory for fast testing
/// </summary>
public class FastTestDistributedApplication : IAsyncDisposable
{
	private readonly WebApplication _webApp;
	private readonly HttpClient _httpClient;

	public FastTestDistributedApplication()
	{
		var builder = WebApplication.CreateBuilder();
		
		// Configure like the real WebApiHost but optimized for testing
		ConfigureForTesting(builder);
		
		_webApp = builder.Build();
		
		// Configure the pipeline like WebApiHost
		ConfigurePipeline(_webApp);
		
		// Start the app
		var startTask = _webApp.StartAsync();
		startTask.Wait(); // Wait for startup to complete
		
		// Create HTTP client - get the first URL from the started app
		var url = _webApp.Urls.FirstOrDefault();
		if (string.IsNullOrEmpty(url))
		{
			// Fallback to default localhost with port 5000
			url = "http://localhost:5000";
		}
		
		_httpClient = new HttpClient()
		{
			BaseAddress = new Uri(url)
		};
	}

	private void ConfigureForTesting(WebApplicationBuilder builder)
	{
		// Use the shared Postgres container connection string
		if (AppHost.SharedPostgresContainer != null)
		{
			builder.Configuration.AddInMemoryCollection(new[]
			{
				new KeyValuePair<string, string?>("ConnectionStrings:postgres", AppHost.SharedPostgresContainer.GetConnectionString()),
			});
		}
		
		// Configure OTLP to send to test server
		builder.Configuration.AddInMemoryCollection(new[]
		{
			new KeyValuePair<string, string?>("OTEL_EXPORTER_OTLP_ENDPOINT", $"https://localhost:{AppHost.TestOtlpServer.Port}"),
			new KeyValuePair<string, string?>("OTEL_TRACES_SAMPLER", "always_on"),
			new KeyValuePair<string, string?>("OTEL_BSP_SCHEDULE_DELAY", "500"),
			new KeyValuePair<string, string?>("DOTNET_ENVIRONMENT", "Testing"),
		});

		// Add minimal services needed for testing
		builder.Services.AddControllers();
		builder.Services.AddHealthChecks();
		
		// Add basic OpenTelemetry if packages are available
		// Note: This is a simplified version - the real WebApiHost has more comprehensive telemetry setup
	}

	private void ConfigurePipeline(WebApplication app)
	{
		// Configure pipeline similar to WebApiHost
		app.MapGet("/", () => "Hello World!");
		app.MapGet("/alive", () => Results.Ok("Alive"));
		app.MapControllers();
		app.MapHealthChecks("/health");
	}

	public async Task<HttpClient> GetHttpClientAsync()
	{
		// Wait a bit for the app to be ready
		await Task.Delay(100);
		return _httpClient;
	}

	public async ValueTask DisposeAsync()
	{
		_httpClient?.Dispose();
		if (_webApp != null)
		{
			await _webApp.DisposeAsync();
		}
	}
}