// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> postgresUserName = builder.AddParameter("dbUser");
IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("dbPassword", secret: true);
IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres", postgresUserName, postgresPassword);

builder.AddProject<Excos_Platform_WebApiHost>("WebApiHost")
	.WaitFor(postgres)
	.WithReference(postgres, "postgres");


builder.Build().Run();