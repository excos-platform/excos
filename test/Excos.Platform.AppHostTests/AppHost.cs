// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Aspire.Hosting;
using Excos.Testing.OpenTelemetry;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
	private readonly WebApplicationFactory<Program> _factory;
	private readonly HttpClient _httpClient;

	public FastTestDistributedApplication()
	{
		this._factory = new CustomWebApplicationFactory();

		// Create the HTTP client from the factory
		this._httpClient = this._factory.CreateClient();
	}

	public async Task<HttpClient> GetHttpClientAsync()
	{
		// WebApplicationFactory ensures the app is ready
		await Task.Delay(10); // Minimal delay just to be safe
		return this._httpClient;
	}

	public async ValueTask DisposeAsync()
	{
		this._httpClient?.Dispose();
		if (this._factory != null)
		{
			await this._factory.DisposeAsync();
		}
	}
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		// Set test environment
		builder.UseEnvironment("Testing");

		// Override configuration for testing
		builder.ConfigureAppConfiguration((context, config) =>
		{
			// Use the shared Postgres container connection string
			if (AppHost.SharedPostgresContainer != null)
			{
				config.AddInMemoryCollection(new[]
				{
					new KeyValuePair<string, string?>("ConnectionStrings:postgres", AppHost.SharedPostgresContainer.GetConnectionString()),
				});
			}

			// Configure OTLP to send to test server
			config.AddInMemoryCollection(new[]
			{
				new KeyValuePair<string, string?>("OTEL_EXPORTER_OTLP_ENDPOINT", $"https://localhost:{AppHost.TestOtlpServer.Port}"),
				new KeyValuePair<string, string?>("OTEL_TRACES_SAMPLER", "always_on"),
				new KeyValuePair<string, string?>("OTEL_BSP_SCHEDULE_DELAY", "500"),
				new KeyValuePair<string, string?>("DOTNET_ENVIRONMENT", "Testing"),
			});
		});
	}
}