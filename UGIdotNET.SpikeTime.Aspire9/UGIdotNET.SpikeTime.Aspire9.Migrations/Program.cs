using UGIdotNET.SpikeTime.Aspire9.Entities;
using UGIdotNET.SpikeTime.Aspire9.Migrations;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddSqlServerDbContext<CatalogDbContext>("spiketime-db");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
