using UGIdotNET.SpikeTime.AspireAzure.Entities;
using UGIdotNET.SpikeTime.AspireAzure.Migrations;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<CatalogDbContext>("spiketime-db");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
