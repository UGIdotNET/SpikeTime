using System.Net.Http.Json;
using VideoHub.Api.Contracts;

namespace VideoHub.Api.Tests;

public class PublicVideoTests(VideoHubApiFactory factory) : IClassFixture<VideoHubApiFactory>
{
    [Fact]
    public async Task Detail_returns_the_video_with_embed_url_and_no_ratings_yet()
    {
        var admin = await factory.AdminClientAsync();
        var created = await admin.CreateVideoAsync("Detail video", "https://youtu.be/dQw4w9WgXcQ");

        var video = await factory.CreateClientWithIp().GetFromJsonAsync<VideoSummary>($"/api/videos/{created.Id}");

        Assert.NotNull(video);
        Assert.Equal(created.Id, video.Id);
        Assert.Equal("Detail video", video.Title);
        Assert.Equal("https://youtu.be/dQw4w9WgXcQ", video.Url);
        Assert.Equal("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", video.EmbedUrl);
        Assert.Equal(0, video.AverageRating);
        Assert.Equal(0, video.RatingCount);
    }

    [Fact]
    public async Task Detail_of_unknown_video_returns_404()
    {
        var response = await factory.CreateClientWithIp().GetAsync($"/api/videos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_is_public_and_returns_newest_first()
    {
        var admin = await factory.AdminClientAsync();
        var first = await admin.CreateVideoAsync();
        await Task.Delay(20);
        var second = await admin.CreateVideoAsync();

        var list = await factory.CreateClientWithIp().GetFromJsonAsync<List<VideoSummary>>("/api/videos?pageSize=50");

        var ids = list!.Select(v => v.Id).ToList();
        Assert.Contains(first.Id, ids);
        Assert.Contains(second.Id, ids);
        Assert.True(ids.IndexOf(second.Id) < ids.IndexOf(first.Id), "newest video must come first");
    }

    [Fact]
    public async Task List_paging_returns_distinct_pages_and_clamps_page_size()
    {
        var admin = await factory.AdminClientAsync();
        for (var i = 0; i < 3; i++)
        {
            await admin.CreateVideoAsync();
            await Task.Delay(5);
        }

        var client = factory.CreateClientWithIp();
        var page1 = await client.GetFromJsonAsync<List<VideoSummary>>("/api/videos?page=1&pageSize=2");
        var page2 = await client.GetFromJsonAsync<List<VideoSummary>>("/api/videos?page=2&pageSize=2");
        var oversized = await client.GetFromJsonAsync<List<VideoSummary>>("/api/videos?pageSize=100000");
        var zero = await client.GetFromJsonAsync<List<VideoSummary>>("/api/videos?page=0&pageSize=0");

        Assert.Equal(2, page1!.Count);
        Assert.NotEmpty(page2!);
        Assert.Empty(page1.Select(v => v.Id).Intersect(page2!.Select(v => v.Id)));
        Assert.True(oversized!.Count <= 50, "page size must be clamped to 50");
        Assert.Single(zero!);
    }

    [Fact]
    public async Task Vimeo_link_gets_a_player_embed_url()
    {
        var admin = await factory.AdminClientAsync();
        var created = await admin.CreateVideoAsync(url: "https://vimeo.com/76979871");

        var video = await factory.CreateClientWithIp().GetFromJsonAsync<VideoSummary>($"/api/videos/{created.Id}");

        Assert.Equal("https://player.vimeo.com/video/76979871", video!.EmbedUrl);
    }
}
