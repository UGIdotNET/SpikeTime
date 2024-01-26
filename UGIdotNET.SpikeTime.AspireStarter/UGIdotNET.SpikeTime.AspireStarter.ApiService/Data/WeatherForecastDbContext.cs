using Microsoft.EntityFrameworkCore;

namespace UGIdotNET.SpikeTime.AspireStarter.ApiService.Data;

public class WeatherForecastDbContext : DbContext
{
    public WeatherForecastDbContext(DbContextOptions<WeatherForecastDbContext> options)
        : base(options)
    {
        
    }

    public DbSet<WeatherForecast> WeatherForecasts { get; set; }
}

public class WeatherForecast
{
    public Guid Id { get; set; }

    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; } 
    
    public string? Summary { get; set; }
}
