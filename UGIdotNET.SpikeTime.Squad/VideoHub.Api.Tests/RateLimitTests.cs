using System.Net.Http.Json;
using VideoHub.Api.Contracts;

namespace VideoHub.Api.Tests;

public class RateLimitTests(VideoHubApiFactory factory) : IClassFixture<VideoHubApiFactory>
{
    private async Task<Guid> NewVideoAsync() => (await (await factory.AdminClientAsync()).CreateVideoAsync()).Id;

    [Fact]
    public async Task Eleventh_public_post_from_one_ip_within_a_minute_gets_429()
    {
        var videoId = await NewVideoAsync();
        var client = factory.CreateClientWithIp();

        for (var i = 0; i < 10; i++)
        {
            var ok = await client.PostAsJsonAsync($"/api/videos/{videoId}/comments", new CommentRequest("Ada", "c" + i, null));
            Assert.Equal(HttpStatusCode.Accepted, ok.StatusCode);
        }

        var limited = await client.PostAsJsonAsync($"/api/videos/{videoId}/comments", new CommentRequest("Ada", "one too many", null));
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        var rating = await client.PostAsJsonAsync($"/api/videos/{videoId}/ratings", new RatingRequest(5));
        Assert.Equal(HttpStatusCode.TooManyRequests, rating.StatusCode);
    }

    [Fact]
    public async Task Limited_requests_are_not_processed()
    {
        var videoId = await NewVideoAsync();
        var client = factory.CreateClientWithIp();
        for (var i = 0; i < 10; i++)
        {
            await client.PostAsJsonAsync($"/api/videos/{videoId}/comments", new CommentRequest("Ada", "c" + i, null));
        }

        await client.PostAsJsonAsync($"/api/videos/{videoId}/comments", new CommentRequest("Late", "dropped", null));

        var admin = await factory.AdminClientAsync();
        var stored = (await admin.AdminCommentsAsync()).Where(c => c.VideoId == videoId).ToList();
        Assert.Equal(10, stored.Count);
        Assert.DoesNotContain(stored, c => c.Author == "Late");
    }

    [Fact]
    public async Task Rate_limit_is_per_ip_so_another_ip_is_unaffected()
    {
        var videoId = await NewVideoAsync();
        var noisy = factory.CreateClientWithIp();
        for (var i = 0; i < 11; i++)
        {
            await noisy.PostAsJsonAsync($"/api/videos/{videoId}/comments", new CommentRequest("Ada", "c" + i, null));
        }

        var other = await factory.CreateClientWithIp()
            .PostAsJsonAsync($"/api/videos/{videoId}/comments", new CommentRequest("Bob", "hi", null));

        Assert.Equal(HttpStatusCode.Accepted, other.StatusCode);
    }

    [Fact]
    public async Task Sixth_login_attempt_from_one_ip_within_a_minute_gets_429_even_with_valid_credentials()
    {
        var client = factory.CreateClientWithIp();

        for (var i = 0; i < 5; i++)
        {
            // Unknown account: counts against the IP limit without locking the real admin account.
            var bad = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody@test.local", "wrong-" + i));
            Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
        }

        var sixth = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(VideoHubApiFactory.AdminEmail, VideoHubApiFactory.AdminPassword));
        Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
    }

    [Fact]
    public async Task Public_reads_are_not_rate_limited()
    {
        var client = factory.CreateClientWithIp();

        for (var i = 0; i < 30; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/videos")).StatusCode);
        }
    }
}
