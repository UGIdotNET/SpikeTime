using System.Net.Http.Json;
using System.Text.Json;
using VideoHub.Api.Contracts;

namespace VideoHub.Api.Tests;

public class VideoUrlValidationTests(VideoHubApiFactory factory) : IClassFixture<VideoHubApiFactory>
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://vimeo.com/76979871")]
    [InlineData("https://player.vimeo.com/video/76979871")]
    [InlineData("HTTPS://WWW.YOUTUBE.COM/watch?v=dQw4w9WgXcQ")]
    public async Task Https_youtube_and_vimeo_links_are_accepted(string url)
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/admin/videos", new VideoRequest("T", "d", url));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("http://vimeo.com/76979871")]
    [InlineData("ftp://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("https://evil.example/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com.evil.example/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://evil.example/youtube.com")]
    [InlineData("https://notyoutube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://user@evil.example@youtube.com.evil.example/")]
    [InlineData("//www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("not a url")]
    [InlineData("https://example.com/video.mp4")]
    public async Task Everything_else_is_rejected_with_a_400_on_url(string url)
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/admin/videos", new VideoRequest("T", "d", url));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("url", out _), $"expected a validation error on 'url' for {url}");
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://vimeo.com/76979871", "https://player.vimeo.com/video/76979871")]
    [InlineData("https://vimeo.com/channels/staffpicks/76979871", "https://player.vimeo.com/video/76979871")]
    [InlineData("https://www.youtube.com/@somechannel", null)]
    [InlineData("https://vimeo.com/about", null)]
    public async Task Embed_url_is_derived_only_for_recognised_link_shapes(string url, string? expectedEmbed)
    {
        var admin = await factory.AdminClientAsync();

        var video = await admin.CreateVideoAsync(url: url);

        Assert.Equal(expectedEmbed, video.EmbedUrl);
    }

    [Fact]
    public async Task Embed_url_never_carries_user_controlled_markup_from_the_video_id()
    {
        var admin = await factory.AdminClientAsync();

        var video = await admin.CreateVideoAsync(url: "https://www.youtube.com/watch?v=abc%22%3E%3Cscript%3E");

        Assert.Null(video.EmbedUrl);
    }
}
