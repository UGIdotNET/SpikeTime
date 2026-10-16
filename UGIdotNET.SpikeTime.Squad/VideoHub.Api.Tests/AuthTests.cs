using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VideoHub.Api.Contracts;

namespace VideoHub.Api.Tests;

public class AuthTests(VideoHubApiFactory factory) : IClassFixture<VideoHubApiFactory>
{
    [Fact]
    public async Task Login_with_admin_credentials_returns_a_bearer_token_that_opens_admin_endpoints()
    {
        var client = factory.CreateClientWithIp();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(VideoHubApiFactory.AdminEmail, VideoHubApiFactory.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.True(body.GetProperty("expiresIn").GetInt64() > 0);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/comments")).StatusCode);
    }

    [Theory]
    [InlineData("admin@test.local", "wrong-password")]
    [InlineData("nobody@test.local", "Adm1n!Test-only")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public async Task Login_with_bad_credentials_returns_401_and_no_token(string? email, string? password)
    {
        var client = factory.CreateClientWithIp();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain("accessToken", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_locks_the_account_after_five_failures_even_with_the_right_password()
    {
        await factory.EnsureNonAdminUserAsync();

        // Different IPs so only account lockout (not the per-IP rate limit) can reject the final attempt.
        for (var i = 0; i < 5; i++)
        {
            var bad = await factory.CreateClientWithIp()
                .PostAsJsonAsync("/api/auth/login", new LoginRequest(VideoHubApiFactory.UserEmail, "bad-" + i));
            Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
        }

        var good = await factory.CreateClientWithIp().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(VideoHubApiFactory.UserEmail, VideoHubApiFactory.UserPassword));

        Assert.Equal(HttpStatusCode.Unauthorized, good.StatusCode);
    }
}
