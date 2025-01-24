// See https://aka.ms/new-console-template for more information
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

Console.WriteLine("Hello, World!");

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var openAIEndpoint = configuration["AzureOpenAI:Endpoint"]!;
var credentialKey = configuration["AzureOpenAI:CredentialKey"]!;

var azureOpenAIClient = new AzureOpenAIClient(
    new Uri(openAIEndpoint),
    new AzureKeyCredential(credentialKey));

IChatClient chatClient = azureOpenAIClient.AsChatClient("spiketime-gpt4o-mini");

//IEmbeddingGenerator<string, Embedding<float>> generator = azureOpenAIClient
//    .AsEmbeddingGenerator("spiketime-gpt4o-mini");

//await SimplePrompt(chatClient);
//await ChatWithAI(chatClient);
//await VectorSearch(generator);
//await InvokeFunction(chatClient);

//Da testare
await ChatWithOllama();

async Task ChatWithOllama()
{
    IChatClient chatClient =
    new OllamaChatClient(new Uri("http://localhost:11434/"), "phi3:mini");

    // Start the conversation with context for the AI model
    List<ChatMessage> chatHistory = new();

    while (true)
    {
        // Get user prompt and add to chat history
        Console.WriteLine("Your prompt:");
        var userPrompt = Console.ReadLine();
        chatHistory.Add(new ChatMessage(ChatRole.User, userPrompt));

        // Stream the AI response and add to chat history
        Console.WriteLine("AI Response:");
        var response = "";
        await foreach (var item in
            chatClient.CompleteStreamingAsync(chatHistory))
        {
            Console.Write(item.Text);
            response += item.Text;
        }
        chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
        Console.WriteLine();
    }
}

async Task InvokeFunction(IChatClient chatClient)
{
    var client = new ChatClientBuilder(chatClient)
        .UseFunctionInvocation()
        .Build();

    var chatOptions = new ChatOptions
    {
        Tools = [AIFunctionFactory.Create((string location, string unit) =>
        {
            // Here you would call a weather API to get the weather for the location
            return "Periods of rain or drizzle, 15 C";
        },
        "get_current_weather",
        "Get the current weather in a given location")]
    };

    List<ChatMessage> chatHistory = [new(ChatRole.System, """
    You are a hiking enthusiast who helps people discover fun hikes in their area. You are upbeat and friendly.
    """)];

    while (true)
    {
        Console.Write("your question: ");
        var question = Console.ReadLine();

        // Weather conversation relevant to the registered function
        chatHistory.Add(new ChatMessage(ChatRole.User, question));

        var response = await client.CompleteAsync(chatHistory, chatOptions);
        Console.WriteLine(response.Message.Text);

        chatHistory.Add(new ChatMessage(ChatRole.Assistant, response.Message.Contents));
    }
}

async Task VectorSearch(IEmbeddingGenerator<string, Embedding<float>> generator)
{
    var cloudServices = new List<CloudService>
    {
        new CloudService
        {
            Key=0,
            Name="Azure App Service",
            Description="Host .NET, Java, Node.js, and Python web applications and APIs in a fully managed Azure service. You only need to deploy your code to Azure. Azure takes care of all the infrastructure management like high availability, load balancing, and autoscaling."
        },
    new CloudService
        {
            Key=1,
            Name="Azure Service Bus",
            Description="A fully managed enterprise message broker supporting both point to point and publish-subscribe integrations. It's ideal for building decoupled applications, queue-based load leveling, or facilitating communication between microservices."
        },
    new CloudService
        {
            Key=2,
            Name="Azure Blob Storage",
            Description="Azure Blob Storage allows your applications to store and retrieve files in the cloud. Azure Storage is highly scalable to store massive amounts of data and data is stored redundantly to ensure high availability."
        },
    new CloudService
        {
            Key=3,
            Name="Microsoft Entra ID",
            Description="Manage user identities and control access to your apps, data, and resources.."
        },
    new CloudService
        {
            Key=4,
            Name="Azure Key Vault",
            Description="Store and access application secrets like connection strings and API keys in an encrypted vault with restricted access to make sure your secrets and your application aren't compromised."
        },
    new CloudService
        {
            Key=5,
            Name="Azure AI Search",
            Description="Information retrieval at scale for traditional and conversational search applications, with security and options for AI enrichment and vectorization."
        }
    };

    var vectorStore = new InMemoryVectorStore();
    var cloudServicesStore = vectorStore.GetCollection<int, CloudService>("cloudServices");
    await cloudServicesStore.CreateCollectionIfNotExistsAsync();

    foreach (var service in cloudServices)
    {
        service.Vector = await generator.GenerateEmbeddingVectorAsync(service.Description);
        await cloudServicesStore.UpsertAsync(service);
    }

    while (true)
    {
        Console.Write("Ask your question about Azure: ");
        var query = Console.ReadLine();

        var embedding = await generator!.GenerateEmbeddingVectorAsync(query);

        var results = await cloudServicesStore.VectorizedSearchAsync(embedding, new VectorSearchOptions()
        {
            Top = 1,
            VectorPropertyName = "Vector"
        });

        await foreach (var result in results.Results)
        {
            Console.WriteLine($"Name: {result.Record.Name}");
            Console.WriteLine($"Description: {result.Record.Description}");
            Console.WriteLine($"Vector match score: {result.Score}");
            Console.WriteLine();
        }
    }
}

async Task ChatWithAI(IChatClient chatClient)
{
    var chatHistory = new List<ChatMessage>{
        new ChatMessage(ChatRole.System, """
        Sei un assistente virtuale che può rispondere a domande sulla formula 1.
        Rispondi esclusivamente in italiano. Se non conosci la risposta, dimmelo.
        Per ogni risposta forniscimi anche la fonte da cui hai recuperato la risposta.
        """)
    };

    while (true)
    {
        Console.Write("Fai la tua domanda: ");
        var userMessage = Console.ReadLine();

        chatHistory.Add(new ChatMessage(ChatRole.User, userMessage));

        Console.WriteLine("AI risponde:");
        var response = string.Empty;

        await foreach (var item in chatClient.CompleteStreamingAsync(chatHistory))
        {
            Console.Write(item.Text);
            response += item.Text;
        }

        chatHistory.Add(new ChatMessage(ChatRole.Assistant, response));
        Console.WriteLine();
    }
}

async Task SimplePrompt(IChatClient client)
{
    var prompt = "Cosa mi sai dire di ASP.NET Core?";
    Console.WriteLine($"user > {prompt}");

    var response = await chatClient.CompleteAsync(prompt, new ChatOptions { MaxOutputTokens = 400 });
    Console.WriteLine($"assistant > {response}");
}

internal class CloudService
{
    [VectorStoreRecordKey]
    public int Key { get; set; }

    [VectorStoreRecordData]
    public string Name { get; set; }

    [VectorStoreRecordData]
    public string Description { get; set; }

    [VectorStoreRecordVector(384, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
