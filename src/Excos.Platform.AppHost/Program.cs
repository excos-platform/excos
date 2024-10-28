using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Excos_Platform_WebApiHost>("WebApiHost");

builder.Build().Run();