// See https://aka.ms/new-console-template for more information
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Hello, Spike time! I'm the sender");

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string queueName = "spiketimesb-queue";
string topicName = "spiketime-topic";

string connectionString = configuration.GetConnectionString("AzureServiceBus")!;

int numberOfMessages = 3;

await using var client = new ServiceBusClient(
    connectionString,
    new ServiceBusClientOptions
    {
        TransportType = ServiceBusTransportType.AmqpWebSockets
    });

//await SendToQueueAsync(client);
await SendToTopicAsync(client);

async Task SendToTopicAsync(ServiceBusClient client)
{
    await using var sender = client.CreateSender(topicName);

    using var messageBatch = await sender.CreateMessageBatchAsync();
    for (int i = 0; i < numberOfMessages; i++)
    {
        if (!messageBatch.TryAddMessage(new ServiceBusMessage($"Spike time message #{i}")))
        {
            throw new Exception($"Cannot add message #{i}");
        }
    }

    try
    {
        await sender.SendMessagesAsync(messageBatch);
        Console.WriteLine("Messages sent!");

        Console.ReadKey();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seding messages: {ex}");
        throw;
    }
}

async Task SendToQueueAsync(ServiceBusClient client)
{
    await using var sender = client.CreateSender(queueName);

    using var messageBatch = await sender.CreateMessageBatchAsync();
    for (int i = 0; i < numberOfMessages; i++)
    {
        if (!messageBatch.TryAddMessage(new ServiceBusMessage($"Spike time message #{i}")))
        {
            throw new Exception($"Cannot add message #{i}");
        }
    }

    try
    {
        await sender.SendMessagesAsync(messageBatch);
        Console.WriteLine("Messages sent!");

        Console.ReadKey();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seding messages: {ex}");
        throw;
    }
}