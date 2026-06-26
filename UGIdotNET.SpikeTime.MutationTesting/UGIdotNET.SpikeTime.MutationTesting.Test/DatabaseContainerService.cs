using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using UGIdotNET.SpikeTime.MutationTesting.Data;

namespace UGIdotNET.SpikeTime.MutationTesting.Test;

public class DatabaseContainerService : IAsyncLifetime
{
    private MsSqlContainer _sqlServerContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string SqlServerConnectionString
    {
        get
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(_sqlServerContainer.GetConnectionString())
            {
                InitialCatalog = "TodoItemsTestDb"
            };

            return connectionStringBuilder.ConnectionString;
        }
    }

    public void ReplaceSqlServerConfiguration(IServiceCollection services)
    {
        var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(DbContextOptions<TodoListDbContext>));
        if (dbContextDescriptor is not null)
        {
            services.Remove(dbContextDescriptor);
        }

        services.AddDbContext<TodoListDbContext>(
            options => options.UseSqlServer(SqlServerConnectionString));
    }

    public async Task DisposeAsync()
    {
        await _sqlServerContainer.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        await _sqlServerContainer.StartAsync();

        var options = new DbContextOptionsBuilder<TodoListDbContext>()
            .UseSqlServer(SqlServerConnectionString)
            .Options;

        using var db = new TodoListDbContext(options);
        await db.Database.MigrateAsync();
    }
}
