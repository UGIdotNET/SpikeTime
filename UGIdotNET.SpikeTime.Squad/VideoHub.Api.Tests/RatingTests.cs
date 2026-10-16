using System.Net.Http.Json;
using System.Text.Json;
using VideoHub.Api.Contracts;

namespace VideoHub.Api.Tests;

public class RatingTests(VideoHubApiFactory factory) : IClassFixture<VideoHubApiFactory>
{
    private async Task<Guid> NewVideoAsync() => (await (await factory.AdminClientAsync()).CreateVideoAsync()).Id;

    private static Task<HttpResponseMessage> Rate(HttpClient client, Guid videoId, int stars) =>
        client.PostAsJsonAsync($"/api/videos/{videoId}/ratings", new RatingRequest(stars));

    private async Task<VideoSummary> Reload(Guid videoId) =>
        (await factory.CreateClientWithIp().GetFromJsonAsync<VideoSummary>($"/api/videos/{videoId}"))!;

    [Fact]
    public async Task First_vote_returns_201_with_the_new_average_and_count()
    {
        var videoId = await NewVideoAsync();

        var response = await Rate(factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId()), videoId, 4);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<RatingSummary>();
        Assert.Equal(4, summary!.AverageRating);
        Assert.Equal(1, summary.RatingCount);
    }

    [Fact]
    public async Task Votes_from_different_voters_and_ips_are_averaged_and_shown_on_the_video()
    {
        var videoId = await NewVideoAsync();

        await Rate(factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId()), videoId, 5);
        await Rate(factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId()), videoId, 2);
        var third = await Rate(factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId()), videoId, 2);

        var summary = await third.Content.ReadFromJsonAsync<RatingSummary>();
        Assert.Equal(3, summary!.RatingCount);
        Assert.Equal(3, summary.AverageRating);
        var video = await Reload(videoId);
        Assert.Equal(3, video.AverageRating);
        Assert.Equal(3, video.RatingCount);
    }

    [Fact]
    public async Task Same_voter_from_another_ip_gets_409_and_the_first_vote_is_untouched()
    {
        var videoId = await NewVideoAsync();
        var voter = VideoHubApiFactory.NewVoterId();
        Assert.Equal(HttpStatusCode.Created, (await Rate(factory.CreateClientWithIp(voterId: voter), videoId, 5)).StatusCode);

        var second = await Rate(factory.CreateClientWithIp(voterId: voter), videoId, 1);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var video = await Reload(videoId);
        Assert.Equal(1, video.RatingCount);
        Assert.Equal(5, video.AverageRating);
    }

    [Fact]
    public async Task Same_ip_with_a_different_voter_gets_409()
    {
        var videoId = await NewVideoAsync();
        var ip = VideoHubApiFactory.NextIp();
        Assert.Equal(HttpStatusCode.Created,
            (await Rate(factory.CreateClientWithIp(ip, VideoHubApiFactory.NewVoterId()), videoId, 3)).StatusCode);

        var second = await Rate(factory.CreateClientWithIp(ip, VideoHubApiFactory.NewVoterId()), videoId, 4);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(1, (await Reload(videoId)).RatingCount);
    }

    [Fact]
    public async Task Same_voter_and_ip_can_still_vote_on_a_different_video()
    {
        var videoA = await NewVideoAsync();
        var videoB = await NewVideoAsync();
        var client = factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId());

        Assert.Equal(HttpStatusCode.Created, (await Rate(client, videoA, 3)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Rate(client, videoB, 3)).StatusCode);
    }

    [Fact]
    public async Task A_vote_cannot_be_changed_by_voting_again_or_by_any_other_verb()
    {
        var videoId = await NewVideoAsync();
        var client = factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId());
        await Rate(client, videoId, 5);

        Assert.Equal(HttpStatusCode.Conflict, (await Rate(client, videoId, 1)).StatusCode);
        foreach (var method in new[] { HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete })
        {
            var response = await client.SendAsync(new HttpRequestMessage(method, $"/api/videos/{videoId}/ratings"));
            Assert.True(response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
                $"{method} on ratings must not be routable, got {(int)response.StatusCode}");
        }

        var video = await Reload(videoId);
        Assert.Equal(5, video.AverageRating);
        Assert.Equal(1, video.RatingCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task Stars_outside_1_to_5_return_400_and_store_nothing(int stars)
    {
        var videoId = await NewVideoAsync();
        var client = factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId());

        var response = await Rate(client, videoId, stars);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("stars", out _), "validation error must be reported on 'stars'");
        Assert.Equal(0, (await Reload(videoId)).RatingCount);
        Assert.Equal(HttpStatusCode.Created, (await Rate(client, videoId, 3)).StatusCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task Boundary_star_values_are_accepted(int stars)
    {
        var videoId = await NewVideoAsync();

        var response = await Rate(factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId()), videoId, stars);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Voting_on_an_unknown_video_returns_404()
    {
        var response = await Rate(factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId()), Guid.NewGuid(), 3);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Without_voter_header_the_api_issues_a_cookie_that_identifies_the_voter_across_ips()
    {
        var videoId = await NewVideoAsync();
        var first = await Rate(factory.CreateClientWithIp(), videoId, 4);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var setCookie = Assert.Single(first.Headers.GetValues("Set-Cookie"), c => c.StartsWith("vh_voter="));
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        var cookie = setCookie.Split(';')[0];

        var again = factory.CreateClientWithIp();
        again.DefaultRequestHeaders.Add("Cookie", cookie);
        var second = await Rate(again, videoId, 1);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Malformed_voter_header_is_not_trusted_as_an_identity()
    {
        var videoId = await NewVideoAsync();

        // "short" fails the 16-64 char id rule, so both requests get a fresh cookie identity and only the IP differs.
        var first = await Rate(factory.CreateClientWithIp(voterId: "short"), videoId, 3);
        var second = await Rate(factory.CreateClientWithIp(voterId: "short"), videoId, 3);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Mine_reports_whether_the_current_voter_has_voted()
    {
        var videoId = await NewVideoAsync();
        var voter = VideoHubApiFactory.NewVoterId();
        var client = factory.CreateClientWithIp(voterId: voter);
        var stranger = factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId());

        Assert.False((await client.GetFromJsonAsync<MyRatingResponse>($"/api/videos/{videoId}/ratings/mine"))!.HasVoted);
        await Rate(client, videoId, 4);

        Assert.True((await client.GetFromJsonAsync<MyRatingResponse>($"/api/videos/{videoId}/ratings/mine"))!.HasVoted);
        Assert.False((await stranger.GetFromJsonAsync<MyRatingResponse>($"/api/videos/{videoId}/ratings/mine"))!.HasVoted);
    }

    [Fact]
    public async Task Admin_ratings_list_shows_individual_votes_filtered_by_video_without_voter_identity()
    {
        var admin = await factory.AdminClientAsync();
        var videoId = (await admin.CreateVideoAsync("Rated video")).Id;
        var otherId = (await admin.CreateVideoAsync()).Id;
        await Rate(factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId()), videoId, 4);
        await Rate(factory.CreateClientWithIp(voterId: VideoHubApiFactory.NewVoterId()), otherId, 1);

        var raw = await admin.GetStringAsync($"/api/admin/ratings?videoId={videoId}");
        var ratings = JsonSerializer.Deserialize<List<AdminRating>>(raw, JsonSerializerOptions.Web)!;

        var rating = Assert.Single(ratings);
        Assert.Equal(videoId, rating.VideoId);
        Assert.Equal("Rated video", rating.VideoTitle);
        Assert.Equal(4, rating.Stars);
        Assert.DoesNotContain("hash", raw, StringComparison.OrdinalIgnoreCase);
    }
}
