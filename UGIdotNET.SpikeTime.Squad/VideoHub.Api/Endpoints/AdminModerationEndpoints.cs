using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Contracts;
using VideoHub.Api.Services;
using VideoHub.Data;

namespace VideoHub.Api.Endpoints;

public static class AdminModerationEndpoints
{
    public static void MapAdminModerationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization(AdminSeeder.AdminRole);

        group.MapGet("/ratings", async (Guid? videoId, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Ratings.AsNoTracking();
            if (videoId is not null)
            {
                query = query.Where(r => r.VideoId == videoId);
            }

            var ratings = await query.OrderByDescending(r => r.CreatedAt).Take(500)
                .Select(r => new AdminRating(r.Id, r.VideoId, r.Video!.Title, r.Stars, r.CreatedAt))
                .ToListAsync(ct);
            return Results.Ok(ratings);
        });

        group.MapGet("/comments", async (string? status, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Comments.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<CommentStatus>(status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["status"] = ["Must be Pending, Approved or Rejected."]
                    });
                }

                query = query.Where(c => c.Status == parsed);
            }

            var comments = await query.OrderByDescending(c => c.CreatedAt).Take(500)
                .Select(c => new AdminComment(c.Id, c.VideoId, c.Video!.Title, c.Author, c.Text, c.Status.ToString(),
                    c.CreatedAt, c.ModeratedAt, c.ModeratedBy))
                .ToListAsync(ct);
            return Results.Ok(comments);
        });

        group.MapPost("/comments/{id:guid}/approve", (Guid id, HttpContext ctx, AppDbContext db, CancellationToken ct) =>
            Moderate(id, CommentStatus.Approved, ctx, db, ct));

        group.MapPost("/comments/{id:guid}/reject", (Guid id, HttpContext ctx, AppDbContext db, CancellationToken ct) =>
            Moderate(id, CommentStatus.Rejected, ctx, db, ct));

        group.MapDelete("/comments/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var deleted = await db.Comments.Where(c => c.Id == id).ExecuteDeleteAsync(ct);
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });
    }

    private static async Task<IResult> Moderate(Guid id, CommentStatus status, HttpContext ctx, AppDbContext db, CancellationToken ct)
    {
        var comment = await db.Comments.Include(c => c.Video).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (comment is null)
        {
            return Results.NotFound();
        }

        comment.Status = status;
        comment.ModeratedAt = DateTimeOffset.UtcNow;
        comment.ModeratedBy = ctx.User.Identity?.Name;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new AdminComment(comment.Id, comment.VideoId, comment.Video!.Title, comment.Author, comment.Text,
            comment.Status.ToString(), comment.CreatedAt, comment.ModeratedAt, comment.ModeratedBy));
    }
}
