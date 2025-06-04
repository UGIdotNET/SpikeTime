using UGIdotNET.SpikeTime.AspireAws.Entities;
using UGIdotNET.SpikeTime.AspireAws.Migrations;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<CatalogDbContext>("spiketime-db");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
