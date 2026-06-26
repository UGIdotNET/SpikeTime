using UGIdotNET.SpikeTime.MutationTesting.Data;
using UGIdotNET.SpikeTime.MutationTesting.Migrations;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults(); 
builder.AddSqlServerDbContext<TodoListDbContext>("spiketime-mutations-db");

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
