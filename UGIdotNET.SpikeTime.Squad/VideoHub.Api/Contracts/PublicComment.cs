namespace VideoHub.Api.Contracts;

public record PublicComment(Guid Id, string Author, string Text, DateTimeOffset CreatedAt);
