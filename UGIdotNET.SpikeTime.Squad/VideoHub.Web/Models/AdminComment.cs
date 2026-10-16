namespace VideoHub.Web.Models;

public record AdminComment(
    Guid Id,
    Guid VideoId,
    string VideoTitle,
    string Author,
    string Text,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModeratedAt,
    string? ModeratedBy);
