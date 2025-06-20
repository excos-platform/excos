// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Excos.Platform.AppHostTests;

public class InMemoryTestApplication : IAsyncDisposable
{
	private readonly HttpClient _httpClient;
	private readonly IServiceScope _scope;
	private readonly PostgreSqlContainer _postgres;

	private InMemoryTestApplication(HttpClient httpClient, IServiceScope scope, PostgreSqlContainer postgres)
	{
		_httpClient = httpClient;
		_scope = scope;
		_postgres = postgres;
	}

	public HttpClient HttpClient => _httpClient;

	public static async Task<InMemoryTestApplication> CreateAsync()
	{
		// Start Postgres container
		var postgres = new PostgreSqlBuilder()
			.WithImage("postgres:15")
			.WithDatabase("excos-db")
			.WithUsername("test-user")
			.WithPassword("test-password")
			.WithCleanUp(true)
			.Build();

		await postgres.StartAsync();

		// Create a simple in-memory web application mimicking the WebApiHost
		var builder = WebApplication.CreateBuilder();
		
		// Override configuration
		builder.Configuration.AddInMemoryCollection(new[]
		{
			new KeyValuePair<string, string?>("ConnectionStrings:postgres", postgres.GetConnectionString()),
			new KeyValuePair<string, string?>("OTEL_EXPORTER_OTLP_ENDPOINT", ""), // Disable OTLP
		});

		// Add only essential services for testing
		builder.Services.AddControllers();
		builder.Services.AddHealthChecks();

		var app = builder.Build();

		// Configure simple pipeline
		app.MapGet("/", () => "Hello World!");
		app.MapControllers();
		app.MapHealthChecks("/health");

		await app.StartAsync();

		var client = new HttpClient()
		{
			BaseAddress = new Uri($"http://localhost:{app.Urls.First().Split(':').Last()}")
		};

		var scope = app.Services.CreateScope();
		return new InMemoryTestApplication(client, scope, postgres);
	}

	public async ValueTask DisposeAsync()
	{
		_httpClient?.Dispose();
		_scope?.Dispose();
		if (_postgres != null)
		{
			await _postgres.DisposeAsync();
		}
	}
}

public static class AppHost
{
	private static PostgreSqlContainer? _sharedPostgresContainer;
	private static readonly SemaphoreSlim _initSemaphore = new(1, 1);

	public static async Task<InMemoryTestApplication> StartAsync()
	{
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

		return await InMemoryTestApplication.CreateAsync();
	}

	public static async Task<HttpClient> GetWebApiClientAsync(this InMemoryTestApplication app)
	{
		// No need to wait for resource states in in-memory testing
		// The HttpClient is already ready to use
		await Task.CompletedTask;
		return app.HttpClient;
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