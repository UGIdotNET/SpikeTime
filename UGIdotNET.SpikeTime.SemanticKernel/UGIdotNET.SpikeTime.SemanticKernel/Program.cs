using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using UGIdotNET.SpikeTime.SemanticKernel;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string endpoint = configuration["AzureOpenAI:Endpoint"]!;
string credential = configuration["AzureOpenAI:Credential"]!;

var builder = Kernel.CreateBuilder();

builder.AddAzureOpenAIChatCompletion("gpt-4o-mini", endpoint, credential);
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

var kernel = builder.Build();

kernel.Plugins.AddFromType<LightsPlugin>("Lights");
kernel.Plugins.AddFromType<PizzaPlugin>("Pizza");

var executionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

var history = new ChatHistory();

string? input;

do
{
    Console.Write("You: ");
    input = Console.ReadLine();
    if (input != null)
    {
        history.AddUserMessage(input);

        var response = await chatCompletion.GetChatMessageContentAsync(
            history,
            executionSettings,
            kernel: kernel);

        Console.WriteLine($"Assistant: {response}");
        
        history.AddMessage(response.Role, response.Content ?? string.Empty);
    }
} while (input != null);

