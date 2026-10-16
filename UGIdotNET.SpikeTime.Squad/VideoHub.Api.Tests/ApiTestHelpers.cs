using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VideoHub.Api.Contracts;

namespace VideoHub.Api.Tests;

public static class ApiTestHelpers
{
    public const string ValidUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

    public static async Task<HttpClient> AdminClientAsync(this VideoHubApiFactory factory) =>
        await factory.LoggedInClientAsync(VideoHubApiFactory.AdminEmail, VideoHubApiFactory.AdminPassword);

    public static async Task<HttpClient> LoggedInClientAsync(this VideoHubApiFactory factory, string email, string password)
    {
        var client = factory.CreateClientWithIp();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<VideoSummary> CreateVideoAsync(this HttpClient admin, string? title = null, string? url = null)
    {
        var response = await admin.PostAsJsonAsync("/api/admin/videos",
            new VideoRequest(title ?? "Video " + Guid.NewGuid().ToString("N")[..8], "desc", url ?? ValidUrl));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<VideoSummary>())!;
    }

    public static async Task<Guid> SubmitCommentAsync(this HttpClient client, Guid videoId, string author, string text)
    {
        var response = await client.PostAsJsonAsync($"/api/videos/{videoId}/comments", new CommentRequest(author, text, null));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CommentCreated>())!.Id;
    }

    public static async Task<List<PublicComment>> PublicCommentsAsync(this HttpClient client, Guid videoId) =>
        (await client.GetFromJsonAsync<List<PublicComment>>($"/api/videos/{videoId}/comments"))!;

    public static async Task<List<AdminComment>> AdminCommentsAsync(this HttpClient admin, string? status = null) =>
        (await admin.GetFromJsonAsync<List<AdminComment>>(
            "/api/admin/comments" + (status is null ? "" : "?status=" + status)))!;
}
