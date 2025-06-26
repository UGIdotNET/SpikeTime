// See https://aka.ms/new-console-template for more information
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using System.Net;

Console.WriteLine("Hello, World!");

//var configuration = new ConfigurationBuilder()
//    .AddUserSecrets<Program>()
//    .Build();

var chain = new CredentialProfileStoreChain();
chain.TryGetAWSCredentials("default", out var credentials);

var snsClient = new AmazonSimpleNotificationServiceClient(
    credentials,
    Amazon.RegionEndpoint.EUNorth1);

var sqsClient = new AmazonSQSClient(
    credentials,
    Amazon.RegionEndpoint.EUNorth1);

var queueUrl = "https://sqs.eu-north-1.amazonaws.com/703376533525/spiketime-queue-1";

Console.WriteLine($"Hello Amazon SNS! Following are some of your topics:");
Console.WriteLine();

var topicsResponse = await snsClient.ListTopicsAsync(
    new ListTopicsRequest());

foreach (var topic in topicsResponse.Topics ?? [])
{
    var attributesResponse = await snsClient.GetTopicAttributesAsync(topic.TopicArn);
    var isFifo = attributesResponse.Attributes.TryGetValue("FifoTopic", out var fifoValue) && fifoValue == "true";

    Console.WriteLine($"\tTopic ARN: {topic.TopicArn}");
    Console.WriteLine();

    if (isFifo)
    {
        var publishRequest = new PublishRequest
        {
            TopicArn = topic.TopicArn,
            Message = $"Ciao da Spike time! Oggi è {DateTime.Now}",
            Subject = "Test Message",
            MessageGroupId = "spiketime-group-1",
            MessageDeduplicationId = Guid.NewGuid().ToString()
        };

        var publishResponse = await snsClient.PublishAsync(publishRequest);

        if (publishResponse.HttpStatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine("Fail!");
            return;
        }

        Console.WriteLine("Message published successfully!");

        await ReadMessagesAsync(sqsClient, queueUrl);
    }
    else
    {
        Console.WriteLine("\tThis is a standard topic.");

        var publishResponse = await snsClient.PublishAsync(
            topic.TopicArn,
            $"Ciao da Spike time! Oggi è il {DateTime.Now}. Questo messaggio ti è stato inviato tramite Amazon SNS");

        if (publishResponse.HttpStatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine("Fail!");
            return;
        }

        Console.WriteLine("Message published successfully!");

        Console.WriteLine("Please check out the subscribed email address");
    }
    
}

async Task ReadMessagesAsync(AmazonSQSClient sqsClient, string queueUrl)
{
    var receiveRequest = new ReceiveMessageRequest
    {
        QueueUrl = queueUrl,
        MaxNumberOfMessages = 10,
        WaitTimeSeconds = 20
    };

    var receiveResponse = await sqsClient.ReceiveMessageAsync(receiveRequest);

    foreach (var message in receiveResponse.Messages)
    {
        Console.WriteLine($"Messaggio ricevuto: {message.Body}");

        // (Opzionale) Cancella il messaggio dalla coda dopo averlo processato
        await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle);
    }
}