// See https://aka.ms/new-console-template for more information
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

Console.WriteLine("Hello, World!");

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"];
var azureOpenAIKey = configuration["AzureOpenAI:Key"];

var gpt52ModelName = "spiketime-mcp-gpt-5.2-chat";

var agent = new AzureOpenAIClient(
    new(azureOpenAIEndpoint!),
    new Azure.AzureKeyCredential(azureOpenAIKey!)).GetChatClient(gpt52ModelName).AsAIAgent();

await foreach (var item in agent.RunStreamingAsync("Suggeriscimi un abstract per un contenuto su YouTube legato a Microsoft Agent Framework"))
{
    Console.Write(item);
}
Console.WriteLine();