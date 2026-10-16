namespace VideoHub.Web.Models;

public record LoginResponse(string TokenType, string AccessToken, int ExpiresIn);
