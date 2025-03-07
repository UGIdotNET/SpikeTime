var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddAzureSqlServer("spiketime-sql")
    .RunAsContainer();

var db = sqlServer.AddDatabase("spiketime-db");

var storage = builder.AddAzureStorage("spiketime-storage")
    .RunAsEmulator();

var blobs = storage.AddBlobs("spiketime-blobs");

var apiService = builder.AddProject<Projects.UGIdotNET_SpikeTime_AspireAzure_ApiService>("apiservice")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(blobs)
    .WaitFor(blobs);

builder.AddProject<Projects.UGIdotNET_SpikeTime_AspireAzure_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.UGIdotNET_SpikeTime_AspireAzure_Migrations>("migrations")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
