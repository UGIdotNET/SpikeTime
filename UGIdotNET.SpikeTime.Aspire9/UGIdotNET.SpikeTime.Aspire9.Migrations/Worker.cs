using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;
using UGIdotNET.SpikeTime.Aspire9.Entities;

namespace UGIdotNET.SpikeTime.Aspire9.Migrations;

public class Worker : BackgroundService
{
    public const string ActivitySourceName = "SpikeTime.Migrations";

    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity("Migrating database", ActivityKind.Client);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

            await EnsureDatabaseAsync(dbContext, stoppingToken);
            await RunMigrationAsync(dbContext, stoppingToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        _hostApplicationLifetime.StopApplication();
    }

    private async Task RunMigrationAsync(CatalogDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private async Task EnsureDatabaseAsync(CatalogDbContext dbContext, CancellationToken cancellationToken)
    {
        var dbCreator = dbContext.GetService<IRelationalDatabaseCreator>();

        // Create the database if it does not exist.
        // Do this first so there is then a database to start a transaction against.
        if (!await dbCreator.ExistsAsync(cancellationToken))
        {
            await dbCreator.CreateAsync(cancellationToken);
        }
    }
}
