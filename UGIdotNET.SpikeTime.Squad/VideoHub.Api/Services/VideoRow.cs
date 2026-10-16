namespace VideoHub.Api.Services;

public record VideoRow(Guid Id, string Title, string Description, string Url, DateTimeOffset CreatedAt, double Average, int Count);
