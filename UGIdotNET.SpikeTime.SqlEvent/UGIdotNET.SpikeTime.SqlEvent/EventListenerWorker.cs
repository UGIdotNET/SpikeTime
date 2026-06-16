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
        
        var dataJson = root.GetProperty("data");
 
        using var innerDoc = JsonDocument.Parse(dataJson.GetString()!);
        var data = innerDoc.RootElement;
            
        var operation = root.GetProperty("operation").GetString();
        var cols = data.GetProperty("eventsource").GetProperty("cols").EnumerateArray();
        var current = JsonSerializer.Deserialize<Dictionary<string, string>>(data.GetProperty("eventrow").GetProperty("current").GetString()!);
        var old = JsonSerializer.Deserialize<Dictionary<string, string>>(data.GetProperty("eventrow").GetProperty("old").GetString()!);

        logger.LogInformation("Operation: {Operation}", operation);

        DeserializeEventMetadata(args, root, data);
        
        if (operation == "INS")
            ProcessInsert(cols, current!);
        
        switch (operation)
        {
            case "INS":
                ProcessInsert(cols, current!);
                break;
            case "UPD":
                ProcessUpdate(cols, current!, old!);
                break;
            case "DEL":
                ProcessDelete(cols, old!);
                break;
        }

        await args.UpdateCheckpointAsync(args.CancellationToken);
    }
    
    private void DeserializeEventMetadata(ProcessEventArgs eventArgs, JsonElement root, JsonElement data)
    {
        logger.LogInformation("Event Args");
        logger.LogInformation($"  Sequence:Offset => {eventArgs.Data.SequenceNumber}:{eventArgs.Data.Offset}");
        
        
        logger.LogInformation("Event Data");
        logger.LogInformation($"  Spec version:       {root.GetProperty("specversion").GetString()}");
        logger.LogInformation($"  Operation:          {root.GetProperty("type").GetString()}");
        logger.LogInformation($"  Time:               {root.GetProperty("time").GetString()}");
        logger.LogInformation($"  Event ID:           {root.GetProperty("id").GetString()}");
        logger.LogInformation($"  Logical ID:         {root.GetProperty("logicalid").GetString()}");
        logger.LogInformation($"  Operation:          {root.GetProperty("operation").GetString()}");
        logger.LogInformation($"  Data content type:  {root.GetProperty("datacontenttype").GetString()}");
        
        logger.LogInformation("Data");
        logger.LogInformation($"  Database:           {data.GetProperty("eventsource").GetProperty("db").GetString()}");
        logger.LogInformation($"  Schema:             {data.GetProperty("eventsource").GetProperty("schema").GetString()}");
        logger.LogInformation($"  Table:              {data.GetProperty("eventsource").GetProperty("tbl").GetString()}");
    }
    
    private void ProcessInsert(JsonElement.ArrayEnumerator cols, Dictionary<string, string> current)
    {
        logger.LogInformation("Processing insert event");
        
        var orderId = string.Empty;
        var customerName = string.Empty;
        var productDescription = string.Empty;
        var status = string.Empty;
        
        foreach (var name in cols.Select(col => col.GetProperty("name").GetString()))
        {
            switch (name)
            {
                case "Id":
                    orderId = current[name];
                    break;
                case "Customer":
                    customerName = current[name];
                    break;
                case "Product":
                    productDescription = current[name];
                    break;
                case "Status":
                    status = current[name];
                    break;
            }
        }

        logger.LogInformation("OrderId: {OrderId}", orderId);
        logger.LogInformation("CustomerName: {CustomerName}", customerName);
        logger.LogInformation("ProductDescription: {ProductDescription}", productDescription);
        logger.LogInformation("Status: {Status}", status);
    }
    
    private void ProcessUpdate(JsonElement.ArrayEnumerator cols, Dictionary<string, string> current, Dictionary<string, string> old)
    {
        logger.LogInformation("Operation: Update");

        foreach (var name in cols.Select(col => col.GetProperty("name").GetString()))
        {
            if (old.Count > 0 && current[name] != old[name])
            {
                logger.LogInformation($"\t{name}: {current[name]} (old: {old[name]})");
            }
            else
            {
                logger.LogInformation($"\t{name}: {current[name]}");
            }
        }
    }
    
    private void ProcessDelete(JsonElement.ArrayEnumerator cols, Dictionary<string, string> old)
    {
        logger.LogInformation("Operation: Delete");

        foreach (var name in cols.Select(col => col.GetProperty("name").GetString()))
        {
            logger.LogInformation($"\t{name}: {old[name]}");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        processorClient.ProcessEventAsync += OnProcessEventAsync;
        processorClient.ProcessErrorAsync += OnProcessErrorAsync;

        await processorClient.StartProcessingAsync(stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        processorClient.ProcessEventAsync -= OnProcessEventAsync;
        processorClient.ProcessErrorAsync -= OnProcessErrorAsync;
        return base.StopAsync(cancellationToken);
    }
}
