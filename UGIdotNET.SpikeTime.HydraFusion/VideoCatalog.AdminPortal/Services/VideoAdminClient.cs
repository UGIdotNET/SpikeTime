using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace VideoCatalog.AdminPortal.Services;

public sealed class VideoAdminClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
{
    public async Task<IReadOnlyList<VideoItem>> GetVideosAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/videos");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<VideoItem>>() ?? [];
    }

    public async Task<VideoItem> CreateVideoAsync(CreateVideoRequest request)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/admin/videos")
        {
            Content = JsonContent.Create(request)
        };

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());

        using var response = await httpClient.SendAsync(httpRequest);
        var payload = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(payload);
        }

        return (await response.Content.ReadFromJsonAsync<VideoItem>())!;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("La sessione HTTP non è disponibile.");
        var token = await context.GetTokenAsync("access_token");

        return !string.IsNullOrWhiteSpace(token)
            ? token
            : throw new InvalidOperationException("Il token di accesso Keycloak non è disponibile.");
    }
}

public sealed record CreateVideoRequest(string Title, string Description, string Url);

public sealed record VideoItem(
    int Id,
    string Title,
    string Description,
    string Url,
    string Platform,
    double AverageRating,
    long RatingsCount);
