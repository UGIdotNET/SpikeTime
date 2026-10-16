#:sdk Aspire.AppHost.Sdk@13.6.1
#:package Aspire.Hosting.PostgreSQL@13.6.1
#:project VideoHub.Api\VideoHub.Api.csproj
#:project VideoHub.Web\VideoHub.Web.csproj
#:property AspireUseCliBundle=true

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();
var db = postgres.AddDatabase("videohub");

var api = builder.AddProject<Projects.VideoHub_Api>("api")
    .WithReference(db)
    .WaitFor(db)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.VideoHub_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();


builder.Build().Run();

