namespace VideoHub.Api.Contracts;

public record VideoSummary(
    Guid Id,
    string Title,
    string Description,
    string Url,
    string? EmbedUrl,
    DateTimeOffset CreatedAt,
    double AverageRating,
    int RatingCount);
