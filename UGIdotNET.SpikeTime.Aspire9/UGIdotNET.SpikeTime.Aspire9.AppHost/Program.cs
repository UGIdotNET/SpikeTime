var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("spiketime-sql")
    .WithDataVolume("spiketime-sql-data")
    .WithLifetime(ContainerLifetime.Persistent);

var db = sqlServer.AddDatabase("spiketime-db");

var dab = builder.AddDataAPIBuilder("dab", "../dab/dab-config.json")
    .WithReference(db)
    .WaitFor(db);

var api = builder.AddProject<Projects.UGIdotNET_SpikeTime_Aspire9_Api>("api")
    .WithReference(db)
    .WaitFor(db);

var web = builder.AddProject<Projects.UGIdotNET_SpikeTime_Aspire9_WebApp>("web")
    .WithReference(api)
    .WaitFor(api);

var reactApp = builder.AddNpmApp("react-app", "../aspire-react")
    .WithReference(dab)
    .WaitFor(dab)
    .WithNpmPackageInstallation()
    .WithExternalHttpEndpoints();

var migrationService = builder.AddProject<Projects.UGIdotNET_SpikeTime_Aspire9_Migrations>("migration-service")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
