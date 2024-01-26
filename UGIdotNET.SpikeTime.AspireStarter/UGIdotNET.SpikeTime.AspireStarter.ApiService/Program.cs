using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UGIdotNET.SpikeTime.AspireStarter.ApiService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<WeatherForecastDbContext>(
    options => options.UseInMemoryDatabase("WeatherForecast-InMemory"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        })
        .ToArray();
    return forecast;
});

app.MapPost(
    "/weatherforecast", 
    (WeatherForecastDbContext db, ILoggerFactory loggerFactory, [FromBody] WeatherForecast model) =>
    {
        var logger = loggerFactory.CreateLogger(nameof(WeatherForecast));

        try
        {
            logger.LogInformation(
                "Saving forecast {Date} - {Temperature} - {Summary}",
                model.Date, model.TemperatureC, model.Summary);

            var entity = new UGIdotNET.SpikeTime.AspireStarter.ApiService.Data.WeatherForecast
            {
                Id = Guid.NewGuid(),
                Date = model.Date,
                Summary = model.Summary,
                TemperatureC = model.TemperatureC
            };

            if (model.Date == DateOnly.MinValue)
            {
                throw new InvalidOperationException("Invalid date");
            }

            db.WeatherForecasts.Add(entity);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving forecast: {ErrorMessage}", ex.Message);
            throw;
        }
        
    });

app.MapDefaultEndpoints();

app.Run();

public record WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
