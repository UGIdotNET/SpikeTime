using VideoHub.Api.Contracts;
using VideoHub.Data;

namespace VideoHub.Api.Services;

public static class VideoProjection
{
    public static IQueryable<VideoRow> WithRatings(this IQueryable<Video> videos) =>
        videos.Select(v => new VideoRow(
            v.Id, v.Title, v.Description, v.Url, v.CreatedAt,
            v.Ratings.Average(r => (double?)r.Stars) ?? 0,
            v.Ratings.Count()));

    public static VideoSummary ToSummary(this VideoRow r) => new(
        r.Id, r.Title, r.Description, r.Url, VideoUrl.ToEmbedUrl(r.Url), r.CreatedAt,
        Math.Round(r.Average, 2), r.Count);
}
