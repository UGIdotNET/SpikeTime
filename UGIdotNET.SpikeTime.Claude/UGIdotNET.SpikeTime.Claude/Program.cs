// See https://aka.ms/new-console-template for more information
using Amazon;
using Amazon.BedrockAgentRuntime;
using Amazon.BedrockAgentRuntime.Model;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime;
using Amazon.Runtime.EventStreams.Utils;
using System.Text;
using System.Text.Json;

Console.WriteLine("Hello, World!");

var credentials = new BasicAWSCredentials(
    "<your access key>",
    "<your secret key>");

var client = new AmazonBedrockAgentRuntimeClient(
    credentials,
    RegionEndpoint.EUWest1);

//var modelId = "anthropic.claude-3-5-sonnet-20240620-v1:0";
var agentId = "<your agent id>";
var agentAliasId = "<your agent alias id>";

while (true)
{
    Console.Write("You: ");
    string? userInput = Console.ReadLine();

    if (string.IsNullOrEmpty(userInput))
        break;

    var request = new InvokeAgentRequest
    {
        AgentId = agentId,
        AgentAliasId = agentAliasId,
        SessionId = Guid.NewGuid().ToString(), // Genera un nuovo ID sessione per ogni richiesta
        InputText = userInput
    };

    try
    {
        var response = await client.InvokeAgentAsync(request);

        // Gestisci la risposta
        await ProcessResponseStream(response.Completion);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

static async Task ProcessResponseStream(Amazon.BedrockAgentRuntime.Model.ResponseStream responseStream)
{
    StringBuilder fullResponse = new StringBuilder();

    while (true)
    {
        var eventStream = await responseStream.ReadEventAsync();
        if (eventStream == null) break;

        if (eventStream.Headers.TryGetValue(":event-type", out var eventType))
        {
            switch (eventType)
            {
                case "completion":
                    if (eventStream.Payload != null)
                    {
                        var payloadString = Encoding.UTF8.GetString(eventStream.Payload);
                        var payload = JsonSerializer.Deserialize<CompletionPayload>(payloadString);
                        if (payload != null && payload.Content != null)
                        {
                            Console.Write(payload.Content);
                            fullResponse.Append(payload.Content);
                        }
                    }
                    break;
                case "completion_stop":
                    Console.WriteLine("\nCompletion finished.");
                    break;
                    // Gestisci altri tipi di eventi se necessario
            }
        }
    }

    Console.WriteLine("\nFull response:");
    Console.WriteLine(fullResponse.ToString());
}

public class CompletionPayload
{
    public string Type { get; set; }
    public string Content { get; set; }
}
