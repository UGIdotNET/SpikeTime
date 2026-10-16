namespace VideoHub.Web.Models;

public record AdminRating(Guid Id, Guid VideoId, string VideoTitle, int Stars, DateTimeOffset CreatedAt);
