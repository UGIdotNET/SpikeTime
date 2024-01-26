var builder = DistributedApplication.CreateBuilder(args);

var apiservice = builder.AddProject<Projects.UGIdotNET_SpikeTime_AspireStarter_ApiService>("apiservice");

builder.AddProject<Projects.UGIdotNET_SpikeTime_AspireStarter_Web>("webfrontend")
    .WithReference(apiservice);

builder.Build().Run();
