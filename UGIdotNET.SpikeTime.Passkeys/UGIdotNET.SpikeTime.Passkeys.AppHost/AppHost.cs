var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("spiketime-passkeys-sql")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("spiketime-passkeys-data")
    .WithDbGate(dbGate =>
    {
        dbGate.WithExplicitStart();
    });

var db = sqlServer.AddDatabase("spiketime-passkeys-db");

builder.AddProject<Projects.UGIdotNET_SpikeTime_Passkeys>("ugidotnet-spiketime-passkeys")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
