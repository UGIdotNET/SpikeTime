using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Contracts;
using VideoHub.Api.Services;
using VideoHub.Data;

namespace VideoHub.Api.Endpoints;

public static class RatingEndpoints
{
    public static void MapRatingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/videos/{id:guid}/ratings");

        group.MapGet("/mine", async (Guid id, HttpContext ctx, AppDbContext db, VoterIdentity voters, CancellationToken ct) =>
        {
            var keys = voters.Resolve(ctx);
            var voted = await db.Ratings.AnyAsync(
                r => r.VideoId == id && (r.VoterHash == keys.VoterHash || r.IpHash == keys.IpHash), ct);
            return Results.Ok(new MyRatingResponse(voted));
        });

        group.MapPost("/", async (Guid id, RatingRequest body, HttpContext ctx, AppDbContext db, VoterIdentity voters, CancellationToken ct) =>
        {
            if (body.Stars is < 1 or > 5)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["stars"] = ["Must be between 1 and 5."] });
            }

            if (!await db.Videos.AnyAsync(v => v.Id == id, ct))
            {
                return Results.NotFound();
            }

            var keys = voters.Resolve(ctx);
            if (keys.NewCookieValue is not null)
            {
                VoterIdentity.SetCookie(ctx, keys.NewCookieValue);
            }

            var already = await db.Ratings.AnyAsync(
                r => r.VideoId == id && (r.VoterHash == keys.VoterHash || r.IpHash == keys.IpHash), ct);
            if (already)
            {
                return Results.Problem("You have already rated this video.", statusCode: StatusCodes.Status409Conflict);
            }

            db.Ratings.Add(new Rating { VideoId = id, Stars = body.Stars, VoterHash = keys.VoterHash, IpHash = keys.IpHash });
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Concurrent duplicate hit the unique indexes.
                return Results.Problem("You have already rated this video.", statusCode: StatusCodes.Status409Conflict);
            }

            var stats = await db.Ratings.Where(r => r.VideoId == id)
                .GroupBy(r => r.VideoId)
                .Select(g => new RatingSummary(Math.Round(g.Average(r => (double)r.Stars), 2), g.Count()))
                .FirstAsync(ct);
            return Results.Created($"/api/videos/{id}", stats);
        }).RequireRateLimiting(RateLimitPolicies.PublicWrites);
    }
}
