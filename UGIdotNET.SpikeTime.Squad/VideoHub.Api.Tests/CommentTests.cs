using System.Net.Http.Json;
using System.Text.Json;
using VideoHub.Api.Contracts;

namespace VideoHub.Api.Tests;

public class CommentTests(VideoHubApiFactory factory) : IClassFixture<VideoHubApiFactory>
{
    private async Task<Guid> NewVideoAsync() => (await (await factory.AdminClientAsync()).CreateVideoAsync()).Id;

    private Task<HttpResponseMessage> Post(Guid videoId, string? author, string? text, string? website = null) =>
        factory.CreateClientWithIp().PostAsJsonAsync($"/api/videos/{videoId}/comments", new CommentRequest(author, text, website));

    [Fact]
    public async Task New_comment_returns_202_pending_and_is_hidden_from_the_public_list()
    {
        var videoId = await NewVideoAsync();

        var response = await Post(videoId, "Ada", "Great video");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CommentCreated>();
        Assert.Equal("Pending", created!.Status);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Empty(await factory.CreateClientWithIp().PublicCommentsAsync(videoId));

        var admin = await factory.AdminClientAsync();
        var stored = Assert.Single(await admin.AdminCommentsAsync("Pending"), c => c.Id == created.Id);
        Assert.Equal(videoId, stored.VideoId);
        Assert.Equal("Ada", stored.Author);
        Assert.Equal("Pending", stored.Status);
        Assert.Null(stored.ModeratedAt);
    }

    [Fact]
    public async Task Honeypot_filled_returns_202_but_stores_nothing()
    {
        var videoId = await NewVideoAsync();
        var admin = await factory.AdminClientAsync();
        var before = (await admin.AdminCommentsAsync()).Count(c => c.VideoId == videoId);

        var response = await Post(videoId, "Bot", "buy cheap stuff", website: "http://spam.example");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("Pending", (await response.Content.ReadFromJsonAsync<CommentCreated>())!.Status);
        var after = (await admin.AdminCommentsAsync()).Where(c => c.VideoId == videoId).ToList();
        Assert.Equal(before, after.Count);
        Assert.DoesNotContain(after, c => c.Author == "Bot");
    }

    [Fact]
    public async Task Comment_on_an_unknown_video_returns_404()
    {
        var response = await Post(Guid.NewGuid(), "Ada", "hello");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Listing_comments_of_an_unknown_video_returns_404()
    {
        var response = await factory.CreateClientWithIp().GetAsync($"/api/videos/{Guid.NewGuid()}/comments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(null, "text", "author")]
    [InlineData("", "text", "author")]
    [InlineData("   ", "text", "author")]
    [InlineData("Ada", null, "text")]
    [InlineData("Ada", "   ", "text")]
    public async Task Missing_or_blank_fields_return_400_naming_the_field(string? author, string? text, string field)
    {
        var videoId = await NewVideoAsync();

        var response = await Post(videoId, author, text);

        await AssertBadRequestOn(response, field);
    }

    [Fact]
    public async Task Author_longer_than_80_characters_returns_400_and_80_is_accepted()
    {
        var videoId = await NewVideoAsync();

        await AssertBadRequestOn(await Post(videoId, new string('a', 81), "ok"), "author");
        Assert.Equal(HttpStatusCode.Accepted, (await Post(videoId, new string('a', 80), "ok")).StatusCode);
    }

    [Fact]
    public async Task Text_longer_than_1000_characters_returns_400_and_1000_is_accepted()
    {
        var videoId = await NewVideoAsync();

        await AssertBadRequestOn(await Post(videoId, "Ada", new string('t', 1001)), "text");
        Assert.Equal(HttpStatusCode.Accepted, (await Post(videoId, "Ada", new string('t', 1000))).StatusCode);
    }

    [Fact]
    public async Task Control_characters_are_rejected()
    {
        var videoId = await NewVideoAsync();

        await AssertBadRequestOn(await Post(videoId, "Ada", "bad\u0000text"), "text");
        await AssertBadRequestOn(await Post(videoId, "A\u0007da", "fine"), "author");
    }

    [Fact]
    public async Task Rejected_requests_are_not_stored()
    {
        var videoId = await NewVideoAsync();
        await Post(videoId, "Ada", new string('t', 1001));

        var admin = await factory.AdminClientAsync();
        Assert.DoesNotContain(await admin.AdminCommentsAsync(), c => c.VideoId == videoId);
    }

    [Fact]
    public async Task Markup_is_stored_raw_and_trimmed_so_the_frontend_must_encode_on_output()
    {
        var videoId = await NewVideoAsync();
        var admin = await factory.AdminClientAsync();
        var id = await factory.CreateClientWithIp().SubmitCommentAsync(videoId, "  <b>Ada</b> ", "  <script>alert(1)</script>  ");
        await admin.PostAsync($"/api/admin/comments/{id}/approve", null);

        var comment = Assert.Single(await factory.CreateClientWithIp().PublicCommentsAsync(videoId));

        Assert.Equal("<b>Ada</b>", comment.Author);
        Assert.Equal("<script>alert(1)</script>", comment.Text);
    }

    [Fact]
    public async Task Public_comment_payload_does_not_leak_moderation_fields()
    {
        var videoId = await NewVideoAsync();
        var admin = await factory.AdminClientAsync();
        var id = await factory.CreateClientWithIp().SubmitCommentAsync(videoId, "Ada", "hi");
        await admin.PostAsync($"/api/admin/comments/{id}/approve", null);

        var json = (await factory.CreateClientWithIp().GetFromJsonAsync<JsonElement>($"/api/videos/{videoId}/comments"))[0];

        var names = json.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();
        Assert.Equal(["author", "createdAt", "id", "text"], names);
    }

    private static async Task AssertBadRequestOn(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out _), $"expected a validation error on '{field}': {errors}");
    }
}
