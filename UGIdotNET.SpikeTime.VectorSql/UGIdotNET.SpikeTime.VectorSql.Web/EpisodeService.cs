using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using UGIdotNET.SpikeTime.VectorSql.Data;

namespace UGIdotNET.SpikeTime.VectorSql.Web;

public class EpisodeService(
    SpikeTimeDbContext dbContext,
    IEmbeddingClient embeddingClient)
{
    private static readonly string filePath = "episodi.json";

    public async Task<EpisodeListItem[]> SearchEpisodesAsync(string text, string topic)
    {
        var searchEmbedding = embeddingClient.GetEmbedding(text);
        var searchVector = new SqlVector<float>(searchEmbedding);

        var episodes = await (from e in dbContext.Episodes.AsNoTracking()
                              let distance = EF.Functions.VectorDistance("cosine", e.Embedding, searchVector)
                              where e.Title.Contains(topic) && distance < 0.20
                              orderby distance
                              select new EpisodeListItem(e.Id, e.Title, e.Description, distance)).Take(6).ToArrayAsync();

        return episodes;
    }

    public async Task SeedEpisodesAsync()
    {
        if (dbContext.Episodes.ToList().Any())
        {
            return;
        }

        var episodesContent = File.ReadAllText(filePath);
        var episodeList = JsonSerializer.Deserialize<EpisodeListModel>(
            episodesContent,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        var episodes = episodeList?.Episodes ?? [];

        foreach (var episode in episodes)
        {
            episode.Id = Guid.NewGuid();

            string embeddingText = $"""
                {episode.Title}

                {episode.Description}
                """;

            var embedding = embeddingClient.GetEmbedding(embeddingText);
            episode.Embedding = new SqlVector<float>(embedding);
        }

        dbContext.Episodes.AddRange(episodes);
        await dbContext.SaveChangesAsync();
    }

    record EpisodeListModel
    {
        public List<Episode> Episodes { get; init; } = [];
    }
}
