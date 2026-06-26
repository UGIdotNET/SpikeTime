var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithDataVolume("spiketime-mutations-data")
    .WithDbGate(containerBuilder =>
    {
        containerBuilder.WithExplicitStart();
    });

var db = sql.AddDatabase("spiketime-mutations-db");

builder.AddProject<Projects.UGIdotNET_SpikeTime_MutationTesting>("webapi")
    .WithReference(db)
    .WaitFor(db);

builder.AddProject<Projects.UGIdotNET_SpikeTime_MutationTesting_Migrations>("migrations")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
