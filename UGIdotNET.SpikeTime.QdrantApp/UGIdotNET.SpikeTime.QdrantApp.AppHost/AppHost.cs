var builder = DistributedApplication.CreateBuilder(args);

var qdrant = builder.AddQdrant("qdrant")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("qdrant-data");

builder.AddProject<Projects.UGIdotNET_SpikeTime_QdrantApp>("ugidotnet-spiketime-qdrantapp")
    .WithReference(qdrant)
    .WaitFor(qdrant);

builder.Build().Run();
