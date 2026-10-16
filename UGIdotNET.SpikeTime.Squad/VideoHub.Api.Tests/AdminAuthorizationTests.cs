using System.Net.Http.Json;
using VideoHub.Api.Contracts;

namespace VideoHub.Api.Tests;

public class AdminAuthorizationTests(VideoHubApiFactory factory) : IClassFixture<VideoHubApiFactory>
{
    private static readonly Guid AnyId = Guid.NewGuid();

    public static TheoryData<string, string> AdminEndpoints => new()
    {
        { "GET", "/api/admin/ratings" },
        { "GET", "/api/admin/comments" },
        { "POST", "/api/admin/videos" },
        { "PUT", $"/api/admin/videos/{AnyId}" },
        { "DELETE", $"/api/admin/videos/{AnyId}" },
        { "POST", $"/api/admin/comments/{AnyId}/approve" },
        { "POST", $"/api/admin/comments/{AnyId}/reject" },
        { "DELETE", $"/api/admin/comments/{AnyId}" },
    };

    private static HttpRequestMessage Request(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new VideoRequest("t", "d", ApiTestHelpers.ValidUrl));
        }

        return request;
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Anonymous_request_to_admin_endpoint_returns_401(string method, string path)
    {
        var response = await factory.CreateClientWithIp().SendAsync(Request(method, path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Invalid_bearer_token_on_admin_endpoint_returns_401(string method, string path)
    {
        var client = factory.CreateClientWithIp();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "not-a-real-token");

        var response = await client.SendAsync(Request(method, path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Authenticated_non_admin_user_gets_403_on_admin_endpoint(string method, string path)
    {
        await factory.EnsureNonAdminUserAsync();
        var user = await factory.LoggedInClientAsync(VideoHubApiFactory.UserEmail, VideoHubApiFactory.UserPassword);

        var response = await user.SendAsync(Request(method, path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Forbidden_non_admin_create_does_not_persist_a_video()
    {
        await factory.EnsureNonAdminUserAsync();
        var user = await factory.LoggedInClientAsync(VideoHubApiFactory.UserEmail, VideoHubApiFactory.UserPassword);
        var title = "forbidden-" + Guid.NewGuid().ToString("N");

        var response = await user.PostAsJsonAsync("/api/admin/videos", new VideoRequest(title, "d", ApiTestHelpers.ValidUrl));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var videos = await user.GetFromJsonAsync<List<VideoSummary>>("/api/videos?pageSize=50");
        Assert.DoesNotContain(videos!, v => v.Title == title);
    }

    [Fact]
    public async Task Non_admin_user_can_still_use_public_endpoints()
    {
        await factory.EnsureNonAdminUserAsync();
        var user = await factory.LoggedInClientAsync(VideoHubApiFactory.UserEmail, VideoHubApiFactory.UserPassword);

        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/videos")).StatusCode);
    }

    [Fact]
    public async Task Admin_can_reach_every_admin_read_endpoint()
    {
        var admin = await factory.AdminClientAsync();

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/ratings")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/comments")).StatusCode);
    }
}
