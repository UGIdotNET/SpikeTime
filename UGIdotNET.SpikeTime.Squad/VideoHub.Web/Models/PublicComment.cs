namespace VideoHub.Web.Models;

public record PublicComment(Guid Id, string Author, string Text, DateTimeOffset CreatedAt);
