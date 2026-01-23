// See https://aka.ms/new-console-template for more information
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;

Console.WriteLine("Hello, Spike time!");

const string deploymentName = "spiketime-mcp-gpt-5.2-chat";

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var azureOpenAIClient = new AzureOpenAIClient(
    new("https://spiketime-mcp-oai.openai.azure.com/"),
    new AzureKeyCredential(configuration["AzureOpenAI:Key"]!));

IChatClient chatClient = new ChatClientBuilder(azureOpenAIClient.GetChatClient(deploymentName).AsIChatClient())
    .UseFunctionInvocation()
    .Build();

var clientTransport = new StdioClientTransport(
    new StdioClientTransportOptions
    {
        Name = "Spike time MCP server",
        Command = "dotnet",
        Arguments = [
            "run",
            "--project",
            "../../../../UGIdotNET.SpikeTime.McpServer/UGIdotNET.SpikeTime.McpServer.csproj"
        ]
    });

McpClient client = await McpClient.CreateAsync(clientTransport);

var mcpTools = await client.ListToolsAsync();

do
{
    Console.WriteLine("Ask a question: ");
    var question = Console.ReadLine();

    var chatOptions = new ChatOptions
    {
        Tools = [.. mcpTools]
    };

    var response = chatClient.GetStreamingResponseAsync(question!, chatOptions);
    await foreach (var item in response)
    {
        Console.Write(item.Text);
    }
    Console.WriteLine();

} while (true);
