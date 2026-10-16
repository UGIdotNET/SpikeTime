using System.Net.Http.Json;
using System.Text.Json;
using VideoHub.Api.Contracts;

namespace VideoHub.Api.Tests;

public class CommentModerationTests(VideoHubApiFactory factory) : IClassFixture<VideoHubApiFactory>
{
    private async Task<(HttpClient Admin, Guid VideoId, Guid CommentId)> PendingCommentAsync(string author = "Ada", string text = "hello")
    {
        var admin = await factory.AdminClientAsync();
        var videoId = (await admin.CreateVideoAsync("Moderated video")).Id;
        var commentId = await factory.CreateClientWithIp().SubmitCommentAsync(videoId, author, text);
        return (admin, videoId, commentId);
    }

    [Fact]
    public async Task Approve_makes_the_comment_public_and_records_who_moderated_it()
    {
        var (admin, videoId, commentId) = await PendingCommentAsync("Ada", "approved text");

        var response = await admin.PostAsync($"/api/admin/comments/{commentId}/approve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var moderated = await response.Content.ReadFromJsonAsync<AdminComment>();
        Assert.Equal("Approved", moderated!.Status);
        Assert.Equal(VideoHubApiFactory.AdminEmail, moderated.ModeratedBy);
        Assert.NotNull(moderated.ModeratedAt);
        Assert.Equal("Moderated video", moderated.VideoTitle);
        var publicComment = Assert.Single(await factory.CreateClientWithIp().PublicCommentsAsync(videoId));
        Assert.Equal(commentId, publicComment.Id);
        Assert.Equal("Ada", publicComment.Author);
        Assert.Equal("approved text", publicComment.Text);
    }

    [Fact]
    public async Task Reject_keeps_the_comment_hidden_but_visible_to_admin_as_rejected()
    {
        var (admin, videoId, commentId) = await PendingCommentAsync();

        var response = await admin.PostAsync($"/api/admin/comments/{commentId}/reject", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Rejected", (await response.Content.ReadFromJsonAsync<AdminComment>())!.Status);
        Assert.Empty(await factory.CreateClientWithIp().PublicCommentsAsync(videoId));
        Assert.Contains(await admin.AdminCommentsAsync("Rejected"), c => c.Id == commentId);
    }

    [Fact]
    public async Task Rejecting_an_approved_comment_hides_it_again()
    {
        var (admin, videoId, commentId) = await PendingCommentAsync();
        await admin.PostAsync($"/api/admin/comments/{commentId}/approve", null);
        Assert.Single(await factory.CreateClientWithIp().PublicCommentsAsync(videoId));

        await admin.PostAsync($"/api/admin/comments/{commentId}/reject", null);

        Assert.Empty(await factory.CreateClientWithIp().PublicCommentsAsync(videoId));
    }

    [Fact]
    public async Task Status_filter_returns_only_matching_comments_and_default_returns_all()
    {
        var (admin, videoId, approvedId) = await PendingCommentAsync();
        var pendingId = await factory.CreateClientWithIp().SubmitCommentAsync(videoId, "Bob", "pending one");
        await admin.PostAsync($"/api/admin/comments/{approvedId}/approve", null);

        var approved = (await admin.AdminCommentsAsync("Approved")).Where(c => c.VideoId == videoId).ToList();
        var pending = (await admin.AdminCommentsAsync("pending")).Where(c => c.VideoId == videoId).ToList();
        var all = (await admin.AdminCommentsAsync()).Where(c => c.VideoId == videoId).ToList();

        Assert.Equal([approvedId], approved.Select(c => c.Id));
        Assert.Equal([pendingId], pending.Select(c => c.Id));
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task Unknown_status_filter_returns_400()
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.GetAsync("/api/admin/comments?status=Bogus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors").TryGetProperty("status", out _));
    }

    [Fact]
    public async Task Delete_removes_the_comment_for_good()
    {
        var (admin, videoId, commentId) = await PendingCommentAsync();
        await admin.PostAsync($"/api/admin/comments/{commentId}/approve", null);

        var response = await admin.DeleteAsync($"/api/admin/comments/{commentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await factory.CreateClientWithIp().PublicCommentsAsync(videoId));
        Assert.DoesNotContain(await admin.AdminCommentsAsync(), c => c.Id == commentId);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/comments/{commentId}")).StatusCode);
    }

    [Theory]
    [InlineData("approve")]
    [InlineData("reject")]
    public async Task Moderating_an_unknown_comment_returns_404(string action)
    {
        var admin = await factory.AdminClientAsync();

        var response = await admin.PostAsync($"/api/admin/comments/{Guid.NewGuid()}/{action}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_an_unknown_comment_returns_404()
    {
        var admin = await factory.AdminClientAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/comments/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Only_approved_comments_are_public_and_are_listed_newest_first()
    {
        var admin = await factory.AdminClientAsync();
        var videoId = (await admin.CreateVideoAsync()).Id;
        var client = factory.CreateClientWithIp();
        var older = await client.SubmitCommentAsync(videoId, "A", "older");
        await Task.Delay(20);
        var newer = await factory.CreateClientWithIp().SubmitCommentAsync(videoId, "B", "newer");
        var hidden = await factory.CreateClientWithIp().SubmitCommentAsync(videoId, "C", "never approved");
        await admin.PostAsync($"/api/admin/comments/{older}/approve", null);
        await admin.PostAsync($"/api/admin/comments/{newer}/approve", null);

        var visible = await factory.CreateClientWithIp().PublicCommentsAsync(videoId);

        Assert.Equal([newer, older], visible.Select(c => c.Id));
        Assert.DoesNotContain(visible, c => c.Id == hidden);
    }
}
