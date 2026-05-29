var builder = DistributedApplication.CreateBuilder(args);

var kurrentDb = builder.AddKurrentDB("kurrentdb")
    .WithDataVolume("kurrentdb-data");

builder.AddProject<Projects.UGIdotNET_SpikeTime_Kurrent>("webapp")
    .WithReference(kurrentDb)
    .WaitFor(kurrentDb);

builder.Build().Run();
