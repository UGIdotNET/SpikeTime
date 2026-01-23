using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UGIdotNET.SpikeTime.McpServer.Tools;

internal class SpikeTimeTools
{
    private readonly ILogger<SpikeTimeTools> _logger;
    private static readonly string filePath = @"<FullPathToFile>\episodi.json";

    private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    };

    public SpikeTimeTools(ILogger<SpikeTimeTools> logger)
    {
        _logger = logger;
    }

    [McpServerTool]
    [Description("Returns the spike time episode matching the specified query")]
    public SpikeTimeEpisode? GetSpikeTimeEpisode(
        [Description("The query by which search the episode")] string query)
    {
        try
        {
            _logger.LogInformation("Searching for episode with query: {Query}", query);
            _logger.LogInformation("Looking for file at path: {FilePath}", Path.GetFullPath(filePath));

            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", Path.GetFullPath(filePath));
                return null;
            }

            var episodesContent = File.ReadAllText(filePath);
            _logger.LogInformation("File content length: {Length}", episodesContent.Length);

            var episodeList = JsonSerializer.Deserialize<EpisodeListModel>(
                episodesContent,
                jsonOptions);

            var episodes = episodeList?.Episodes ?? [];
            _logger.LogInformation("Total episodes found: {Count}", episodes.Count);

            var result = episodes
                .FirstOrDefault(e => e.Title.Contains(query, StringComparison.InvariantCultureIgnoreCase) || 
                                    e.Description.Contains(query, StringComparison.InvariantCultureIgnoreCase));

            if (result != null)
            {
                _logger.LogInformation("Episode found: {Title}", result.Title);
            }
            else
            {
                _logger.LogWarning("No episode found for query: {Query}", query);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for episode with query: {Query}", query);
            throw;
        }
    }
}

public class EpisodeListModel
{
    [JsonPropertyName("episodes")]
    public List<SpikeTimeEpisode> Episodes { get; set; } = [];
}

public record SpikeTimeEpisode
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
