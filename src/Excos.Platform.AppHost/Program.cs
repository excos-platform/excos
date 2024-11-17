// Copyright (c) Marian Dziubiak.
// Licensed under the GNU Affero General Public License v3.

using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Excos_Platform_WebApiHost>("WebApiHost");

builder.Build().Run();