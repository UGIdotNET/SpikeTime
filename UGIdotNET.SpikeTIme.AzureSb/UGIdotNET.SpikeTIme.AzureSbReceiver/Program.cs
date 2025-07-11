// See https://aka.ms/new-console-template for more information
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;

Console.WriteLine("Hello, Spike time! ? I'm the receiver");

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string queueName = "spiketimesb-queue";
string topicName = "spiketime-topic";

string[] subscriptions = ["spiketime-topic-sub1", "spiketime-topic-sub2", "spiketime-topic-sub3"];

string connectionString = configuration.GetConnectionString("AzureServiceBus")!;

await using var client = new ServiceBusClient(
    connectionString,
    new ServiceBusClientOptions
    {
        TransportType = ServiceBusTransportType.AmqpWebSockets
    });

//await ReceiveFromQueueAsync(client);

foreach (var subscription in subscriptions)
{
    await ReceiveFromTopicAsync(client, topicName, subscription);
}

Console.ReadKey();

async Task ReceiveFromTopicAsync(ServiceBusClient client, string topicName, string subscriptionName)
{
    await using var processor = client.CreateProcessor(
        topicName, 
        subscriptionName, 
        new ServiceBusProcessorOptions());

    try
    {
        processor.ProcessMessageAsync += OnSubscriptionMessageReceived;
        processor.ProcessErrorAsync += OnSubscriptionErrorReceived;

        Console.WriteLine($"Start processing messages for subscription {subscriptionName}...");
        await processor.StartProcessingAsync();

        await Task.Delay(10000);

        await processor.StopProcessingAsync();
        Console.WriteLine("Stop processing messages...");
    }
    finally
    {
        processor.ProcessMessageAsync -= OnSubscriptionMessageReceived;
        processor.ProcessErrorAsync -= OnSubscriptionErrorReceived;
    }
}

async Task ReceiveFromQueueAsync(ServiceBusClient client)
{
    await using var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions());

    try
    {
        processor.ProcessMessageAsync += OnQueueMessageReceived;
        processor.ProcessErrorAsync += OnQueueErrorReceived;

        Console.WriteLine("Start processing messages...");
        await processor.StartProcessingAsync();

        await Task.Delay(30000);

        await processor.StopProcessingAsync();
        Console.WriteLine("Stop processing messages...");

        Console.ReadKey();
    }
    finally
    {
        processor.ProcessMessageAsync -= OnQueueMessageReceived;
        processor.ProcessErrorAsync -= OnQueueErrorReceived;
    }
}

Task OnQueueErrorReceived(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}

async Task OnQueueMessageReceived(ProcessMessageEventArgs args)
{
    string body = args.Message.Body.ToString();
    Console.WriteLine($"Received: {body}");

    // complete the message. message is deleted from the queue. 
    await args.CompleteMessageAsync(args.Message);
}

Task OnSubscriptionErrorReceived(ProcessErrorEventArgs args)
{
    Console.WriteLine(args.Exception.ToString());
    return Task.CompletedTask;
}

async Task OnSubscriptionMessageReceived(ProcessMessageEventArgs args)
{
    string body = args.Message.Body.ToString();
    Console.WriteLine($"Received: {body}");

    // complete the message. message is deleted from the queue. 
    await args.CompleteMessageAsync(args.Message);
}