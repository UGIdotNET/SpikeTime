using System.Net.Http.Json;
using System.Text.Json;
using VideoHub.Api.Contracts;

namespace VideoHub.Api.Tests;

public class AdminVideoTests(VideoHubApiFactory factory) : IClassFixture<VideoHubApiFactory>
{
    [Fact]
    public async Task Create_returns_201_with_location_and_the_video_is_publicly_visible()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/admin/videos",
            new VideoRequest("  New title  ", "  A description ", "  " + ApiTestHelpers.ValidUrl + " "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<VideoSummary>();
        Assert.Equal($"/api/videos/{created!.Id}", response.Headers.Location!.OriginalString);
        Assert.Equal("New title", created.Title);
        Assert.Equal("A description", created.Description);
        Assert.Equal(ApiTestHelpers.ValidUrl, created.Url);
        Assert.Equal("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", created.EmbedUrl);
        var publicView = await factory.CreateClientWithIp().GetFromJsonAsync<VideoSummary>($"/api/videos/{created.Id}");
        Assert.Equal(created.Title, publicView!.Title);
    }

    [Fact]
    public async Task Description_is_optional()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/admin/videos", new VideoRequest("T", null, ApiTestHelpers.ValidUrl));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("", (await response.Content.ReadFromJsonAsync<VideoSummary>())!.Description);
    }

    [Theory]
    [InlineData(null, "title")]
    [InlineData("", "title")]
    [InlineData("   ", "title")]
    public async Task Create_without_title_returns_400(string? title, string field)
    {
        var admin = await factory.AdminClientAsync();

        await AssertBadRequestOn(await admin.PostAsJsonAsync("/api/admin/videos",
            new VideoRequest(title, "d", ApiTestHelpers.ValidUrl)), field);
    }

    [Fact]
    public async Task Create_without_url_returns_400()
    {
        var admin = await factory.AdminClientAsync();

        await AssertBadRequestOn(await admin.PostAsJsonAsync("/api/admin/videos", new VideoRequest("T", "d", null)), "url");
    }

    [Fact]
    public async Task Field_length_limits_are_enforced_at_the_boundary()
    {
        var admin = await factory.AdminClientAsync();
        var longUrl = "https://www.youtube.com/watch?v=abc&x=" + new string('a', 2000);

        await AssertBadRequestOn(await admin.PostAsJsonAsync("/api/admin/videos",
            new VideoRequest(new string('t', 201), "d", ApiTestHelpers.ValidUrl)), "title");
        await AssertBadRequestOn(await admin.PostAsJsonAsync("/api/admin/videos",
            new VideoRequest("T", new string('d', 2001), ApiTestHelpers.ValidUrl)), "description");
        await AssertBadRequestOn(await admin.PostAsJsonAsync("/api/admin/videos",
            new VideoRequest("T", "d", longUrl)), "url");
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/admin/videos",
            new VideoRequest(new string('t', 200), new string('d', 2000), ApiTestHelpers.ValidUrl))).StatusCode);
    }

    [Fact]
    public async Task Update_changes_fields_and_keeps_existing_ratings()
    {
        var admin = await factory.AdminClientAsync();
        var video = await admin.CreateVideoAsync("Old");
        await factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId())
            .PostAsJsonAsync($"/api/videos/{video.Id}/ratings", new RatingRequest(4));

        var response = await admin.PutAsJsonAsync($"/api/admin/videos/{video.Id}",
            new VideoRequest("Renamed", "new desc", "https://vimeo.com/76979871"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<VideoSummary>();
        Assert.Equal(video.Id, updated!.Id);
        Assert.Equal("Renamed", updated.Title);
        Assert.Equal("new desc", updated.Description);
        Assert.Equal("https://player.vimeo.com/video/76979871", updated.EmbedUrl);
        Assert.Equal(1, updated.RatingCount);
        Assert.Equal(4, updated.AverageRating);
    }

    [Fact]
    public async Task Update_with_an_invalid_url_returns_400_and_leaves_the_video_unchanged()
    {
        var admin = await factory.AdminClientAsync();
        var video = await admin.CreateVideoAsync("Keep me");

        var response = await admin.PutAsJsonAsync($"/api/admin/videos/{video.Id}",
            new VideoRequest("Changed", "d", "http://youtube.com/watch?v=abc"));

        await AssertBadRequestOn(response, "url");
        var current = await factory.CreateClientWithIp().GetFromJsonAsync<VideoSummary>($"/api/videos/{video.Id}");
        Assert.Equal("Keep me", current!.Title);
        Assert.Equal(video.Url, current.Url);
    }

    [Fact]
    public async Task Update_of_unknown_video_returns_404()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PutAsJsonAsync($"/api/admin/videos/{Guid.NewGuid()}",
            new VideoRequest("T", "d", ApiTestHelpers.ValidUrl));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_the_video_together_with_its_ratings_and_comments()
    {
        var admin = await factory.AdminClientAsync();
        var video = await admin.CreateVideoAsync();
        await factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId())
            .PostAsJsonAsync($"/api/videos/{video.Id}/ratings", new RatingRequest(5));
        var commentId = await factory.CreateClientWithIp().SubmitCommentAsync(video.Id, "Ada", "bye");

        var response = await admin.DeleteAsync($"/api/admin/videos/{video.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClientWithIp().GetAsync($"/api/videos/{video.Id}")).StatusCode);
        Assert.Empty(await admin.GetFromJsonAsync<List<AdminRating>>($"/api/admin/ratings?videoId={video.Id}") ?? []);
        Assert.DoesNotContain(await admin.AdminCommentsAsync(), c => c.Id == commentId);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/videos/{video.Id}")).StatusCode);
    }

    private static async Task AssertBadRequestOn(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out _), $"expected a validation error on '{field}': {errors}");
    }
}
