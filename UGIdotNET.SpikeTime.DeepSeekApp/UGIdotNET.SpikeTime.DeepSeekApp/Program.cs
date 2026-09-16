using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== Smart Communications Hub ===");
Console.WriteLine("An AI agent for drafting, analyzing, and summarizing professional communications.");
Console.WriteLine();

// Setup
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();

var foundryEndpoint = configuration["Foundry:Endpoint"]!;
var apiKey = configuration["Foundry:ApiKey"]!;

var connection = new Uri(foundryEndpoint);
var credential = new Azure.AzureKeyCredential(apiKey);

// Create IChatClient for both models
var proClient = new AzureOpenAIClient(connection, credential)
    .GetChatClient("DeepSeek-V4-Pro")
    .AsIChatClient();

var flashClient = new AzureOpenAIClient(connection, credential)
    .GetChatClient("DeepSeek-V4-Flash")
    .AsIChatClient();

// Configure agents
var strategistAgent = new ChatClientAgent(
    proClient,
    instructions: """
        You are a Senior Communications Strategist. Your job is to:
        - Draft professional emails, memos, and messages.
        - Analyze tone (formal, urgent, friendly, assertive).
        - Detect sentiment and suggest improvements.
        - Give thoughtful, structured advice.
        - Always respond in the user's language.
        """,
    name: "Strategist",
    description: "DeepSeek V4 Pro -- deep reasoning and drafting",
    tools: null,
    services: null);

var editorAgent = new ChatClientAgent(
    flashClient,
    instructions: """
        You are a fast, efficient Communications Editor. Your job is to:
        - Proofread text for grammar, spelling, and clarity.
        - Summarize long messages concisely.
        - Suggest quick edits and rephrase sentences.
        - Keep responses brief and to the point.
        """,
    name: "Editor",
    description: "DeepSeek V4 Flash -- fast proofreading and summaries",
    tools: null,
    services: null);

// Interactive loop
Console.WriteLine("Available commands:");
Console.WriteLine("  draft <topic>      -> Strategist drafts a professional email");
Console.WriteLine("  analyze <text>     -> Strategist analyzes tone and sentiment");
Console.WriteLine("  proofread <text>   -> Editor proofreads the text");
Console.WriteLine("  summarize <text>   -> Editor summarizes the text");
Console.WriteLine("  <anything else>    -> Strategist handles it");
Console.WriteLine("  quit               -> Exit");
Console.WriteLine();

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
        break;

    var spaceIndex = input.IndexOf(' ');
    var command = spaceIndex > 0 ? input[..spaceIndex].ToLowerInvariant() : "";
    var payload = spaceIndex > 0 ? input[(spaceIndex + 1)..] : input;

    var (agent, prompt) = (command, payload) switch
    {
        ("draft", _)      => (strategistAgent, $"Draft a professional email about: {payload}"),
        ("analyze", _)    => (strategistAgent, $"Analyze the tone, sentiment, and suggest improvements for this text: {payload}"),
        ("proofread", _)  => (editorAgent, $"Proofread the following text and suggest corrections: {payload}"),
        ("summarize", _)  => (editorAgent, $"Provide a concise summary of the following: {payload}"),
        _                 => (strategistAgent, input),
    };

    try
    {
        var session = await agent.CreateSessionAsync(
            conversationId: $"{Guid.NewGuid()}", CancellationToken.None);

        Console.Write("  [{0}]: ", agent.Name);

        await foreach (var update in agent.RunStreamingAsync(
            prompt,
            session,
            options: null,
            CancellationToken.None))
        {
            if (update.Role == ChatRole.Assistant && !string.IsNullOrWhiteSpace(update.Text))
            {
                Console.Write(update.Text);
            }
        }

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine("  Error: {0}", ex.Message);
    }

    Console.WriteLine();
}

// NOTE: Set endpoint and key via:
//   dotnet user-secrets set "Foundry:Endpoint" "https://<your-foundry>.services.ai.azure.com"
//   dotnet user-secrets set "Foundry:ApiKey" "<your-key>"
