namespace VideoHub.Api.Contracts;

// Website is a honeypot: real users leave it empty.
public record CommentRequest(string? Author, string? Text, string? Website);
