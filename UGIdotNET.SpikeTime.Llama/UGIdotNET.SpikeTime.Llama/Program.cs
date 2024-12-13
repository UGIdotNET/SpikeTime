using Azure;
using Azure.Identity;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Azure.AI.Inference;

string endpoint = "<your model endpoint>";

Console.WriteLine("Hello, Spike time!");

var openAIClient = new AzureOpenAIClient(
    new(endpoint),
    new AzureKeyCredential("<your model key>"));

var chatClient = openAIClient.GetChatClient("Meta-Llama-3.1-8B-Instruct");

ChatCompletion completion = chatClient.CompleteChat(
        [
            new SystemChatMessage("You are an AI assistant that helps people find information."),
            new UserChatMessage("Hi, how are you?"),
        ],
        new ChatCompletionOptions()
      );

Console.WriteLine($"{completion.Role}: {completion.Content}");