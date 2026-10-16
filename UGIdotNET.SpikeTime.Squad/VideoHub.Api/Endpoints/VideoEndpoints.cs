using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Services;
using VideoHub.Data;

namespace VideoHub.Api.Endpoints;

public static class VideoEndpoints
{
    public static void MapVideoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/videos");

        group.MapGet("/", async (AppDbContext db, int? page, int? pageSize, CancellationToken ct) =>
        {
            var size = Math.Clamp(pageSize ?? 20, 1, 50);
            var skip = (Math.Max(page ?? 1, 1) - 1) * size;
            var rows = await db.Videos.AsNoTracking()
                .OrderByDescending(v => v.CreatedAt)
                .Skip(skip).Take(size)
                .WithRatings()
                .ToListAsync(ct);
            return Results.Ok(rows.Select(r => r.ToSummary()));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var row = await db.Videos.AsNoTracking().Where(v => v.Id == id).WithRatings().FirstOrDefaultAsync(ct);
            return row is null ? Results.NotFound() : Results.Ok(row.ToSummary());
        });
    }
}
