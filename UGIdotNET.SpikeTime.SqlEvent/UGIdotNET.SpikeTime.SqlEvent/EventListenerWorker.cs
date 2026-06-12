using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using System.Text.Json;

namespace UGIdotNET.SpikeTime.SqlEvent;

public class EventListenerWorker : BackgroundService
{
    private readonly EventProcessorClient processorClient;
    private readonly ILogger<EventListenerWorker> logger;

    public EventListenerWorker(
        EventProcessorClient processorClient,
        ILogger<EventListenerWorker> logger)
    {
        this.processorClient = processorClient;
        this.logger = logger;
    }

    private async Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError("Error processing event: {@Exception}", args.Exception);
    }

    private async Task OnProcessEventAsync(ProcessEventArgs args)
    {
        using var json = JsonDocument.Parse(args.Data.EventBody.ToArray());
        var root = json.RootElement;

        var operation = root.GetProperty("operation").GetString();
        logger.LogInformation("Operation: {Operation}", operation);

        //eventsource -> cols [] -> name, index, type
        var data = root.GetProperty("data");
        //using var dataDocument = JsonDocument.Parse(data.GetString()!);
        //var dataElement = dataDocument.RootElement;

        //using var eventSourceJson = JsonDocument.Parse(dataElement.GetProperty("eventsource").GetString()!);
        //var cols = eventSourceJson.RootElement.GetProperty("cols").EnumerateArray();
        //var current = JsonSerializer.Deserialize<Dictionary<string, string>>(
        //    data.GetProperty("eventrow").GetProperty("current").GetString()!);

        //var old = JsonSerializer.Deserialize<Dictionary<string, string>>(
        //    data.GetProperty("eventrow").GetProperty("old").GetString()!);

        //logger.LogInformation("Current values {@Current}", current);
        //logger.LogInformation("Current values {@Old}", old);

        //foreach (var col in cols)
        //{
        //    var columnName = col.GetProperty("name").GetString();
        //    var columnType = col.GetProperty("type").GetString();
        //    logger.LogInformation("Column: {ColumnName} ({ColumnType})", columnName, columnType);
        //}

        await args.UpdateCheckpointAsync(args.CancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        processorClient.ProcessEventAsync += OnProcessEventAsync;
        processorClient.ProcessErrorAsync += OnProcessErrorAsync;

        await processorClient.StartProcessingAsync();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        processorClient.ProcessEventAsync -= OnProcessEventAsync;
        processorClient.ProcessErrorAsync -= OnProcessErrorAsync;
        return base.StopAsync(cancellationToken);
    }
}
