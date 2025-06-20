// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> postgresUserName = builder.AddParameter("dbUser");
IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("dbPassword", secret: true);

// default if unspecified
postgresUserName.Resource.Default = new GenerateParameterDefault { MinLength = 3 };
postgresPassword.Resource.Default = new GenerateParameterDefault { MinLength = 8 };

// Check if we're in testing mode
bool isTesting = builder.Environment.EnvironmentName == "Testing";

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres", postgresUserName, postgresPassword, port: null);

// Marten and Wolverine are having a race condition on startup to create the database schema.
// To mitigate this, we are using a custom init script to create the schema ahead of time.
string dbName = "excos-db";
IResourceBuilder<PostgresDatabaseResource> db = postgres
	.WithEnvironment("POSTGRES_DB", dbName)
	.WithBindMount("db-init.sql", "/docker-entrypoint-initdb.d/db-init.sql")
	.AddDatabase(dbName);

IResourceBuilder<ProjectResource> webApiHost = builder.AddProject<Excos_Platform_WebApiHost>("WebApiHost")
	.WaitFor(postgres)
	.WithReference(db, "postgres");

// Add logging configuration for testing
if (isTesting)
{
	webApiHost.WithEnvironment("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
}

builder.Build().Run();