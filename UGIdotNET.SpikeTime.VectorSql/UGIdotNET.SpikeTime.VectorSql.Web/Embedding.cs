using Azure;
using Azure.AI.OpenAI;

namespace UGIdotNET.SpikeTime.VectorSql.Web;

public interface IEmbeddingClient
{
    float[] GetEmbedding(string text);
}

public class AzureOpenAIEmbeddingClient : IEmbeddingClient
{
    private readonly AzureOpenAIClient _azureOpenAIClient;

    private readonly string _deploymentName;

    public AzureOpenAIEmbeddingClient(IConfiguration configuration)
    {
        _azureOpenAIClient = new AzureOpenAIClient(
            new(configuration["AzureOpenAI:Endpoint"]!),
            new AzureKeyCredential(configuration["AzureOpenAI:Key"]!));

        _deploymentName = configuration["AzureOpenAI:DeploymentName"]!;
    }

    public float[] GetEmbedding(string text)
    {
        var embeddingClient = _azureOpenAIClient.GetEmbeddingClient(_deploymentName);

        var embedding = embeddingClient.GenerateEmbedding(text);

        var vector = embedding.Value.ToFloats().ToArray();

        return vector;
    }
}
