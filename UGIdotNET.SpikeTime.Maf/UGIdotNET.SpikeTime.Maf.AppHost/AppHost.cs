var builder = DistributedApplication.CreateBuilder(args);

var writer = builder.AddProject<Projects.UGIdotNET_SpikeTime_Maf_Writer>("writer");

var webapp = builder.AddProject<Projects.UGIdotNET_SpikeTime_Maf_Web>("webapp")
    .WithReference(writer)
    .WaitFor(writer);

builder.Build().Run();
