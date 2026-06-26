using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;
using UGIdotNET.SpikeTime.MutationTesting.Data;

namespace UGIdotNET.SpikeTime.MutationTesting.Migrations;

public class Worker(
    ILogger<Worker> logger,
    IHostApplicationLifetime hostApplicationLifetime,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private static readonly ActivitySource activitySource = new("UGIdotNET.SpikeTime.MutationTesting.Migrations.Worker");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = activitySource.StartActivity("Execute Migrations");

        using var scope = serviceScopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<TodoListDbContext>();
        await EnsureDatabaseIsCreatedAsync(db, stoppingToken);

        logger.LogInformation("Applying all pending migrations...");
        await db.Database.MigrateAsync(stoppingToken).ConfigureAwait(false);
        logger.LogInformation("Migrations applied correctly!");

        hostApplicationLifetime.StopApplication();
    }

    private async Task EnsureDatabaseIsCreatedAsync(TodoListDbContext db, CancellationToken stoppingToken)
    {
        var creator = db.GetService<IRelationalDatabaseCreator>();
        if (await creator.ExistsAsync(stoppingToken))
        {
            logger.LogInformation("Database already exist!");
            return;
        }

        logger.LogInformation("Creating database...");
        await creator.CreateAsync(stoppingToken);
        logger.LogInformation("Database created successfully.");
    }
}
