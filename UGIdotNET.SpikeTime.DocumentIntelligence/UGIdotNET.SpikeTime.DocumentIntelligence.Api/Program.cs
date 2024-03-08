using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.AspNetCore.Mvc;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped(
    sp =>
    {
        string documentIntelligenceEndpoint = "<your document intelligence endpoint>";
        string documentIntelligenceKey = "<your document intelligecence key>";

        return new DocumentAnalysisClient(
            new Uri(documentIntelligenceEndpoint),
            new Azure.AzureKeyCredential(documentIntelligenceKey));
    });

builder.Services.AddCors(
    options => options.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.MapPost(
    "/api/upload",
    async (DocumentAnalysisClient documentClient, [FromForm] IFormFile file) =>
    {
        using var fileStream = file.OpenReadStream();

        var result = await documentClient.AnalyzeDocumentAsync(
            Azure.WaitUntil.Completed,
            "prebuilt-receipt",
            fileStream);

        if (result.HasValue)
        {
            var document = result.Value.Documents.First();

            document.Fields.TryGetValue("MerchantName", out var merchantField);
            document.Fields.TryGetValue("Total", out var totalAmountField);
            document.Fields.TryGetValue("TransactionDate", out var transactionDateField);
            document.Fields.TryGetValue("Items", out var itemsField);

            var receiptItems = itemsField?.Value.AsList() ?? [];
            receiptItems
                .First()
                .Value.AsDictionary()
                .TryGetValue("Description", out var descriptionField);

            var model = new Receipt(
                merchantField?.Value.AsString() ?? string.Empty,
                totalAmountField?.Value.AsDouble() ?? 0,
                transactionDateField?.Value.AsDate().Date,
                descriptionField?.Value.AsString() ?? string.Empty);

            return Results.Ok(model);
        }

        return Results.BadRequest("Error");
    }).DisableAntiforgery();

app.Run();

internal record Receipt(
    string MerchantName,
    double TotalAmount,
    DateTime? TransactionDate,
    string Description);

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
