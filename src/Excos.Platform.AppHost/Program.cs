// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres");

builder.AddProject<Excos_Platform_WebApiHost>("WebApiHost")
	.WaitFor(postgres)
	.WithReference(postgres, "postgres");


builder.Build().Run();