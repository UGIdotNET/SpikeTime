using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Contracts;
using VideoHub.Api.Services;
using VideoHub.Data;

namespace VideoHub.Api.Endpoints;

public static class CommentEndpoints
{
    public static void MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/videos/{id:guid}/comments");

        group.MapGet("/", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            if (!await db.Videos.AnyAsync(v => v.Id == id, ct))
            {
                return Results.NotFound();
            }

            var comments = await db.Comments.AsNoTracking()
                .Where(c => c.VideoId == id && c.Status == CommentStatus.Approved)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new PublicComment(c.Id, c.Author, c.Text, c.CreatedAt))
                .ToListAsync(ct);
            return Results.Ok(comments);
        });

        group.MapPost("/", async (Guid id, CommentRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var errors = Validation.Comment(body.Author, body.Text);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            if (!await db.Videos.AnyAsync(v => v.Id == id, ct))
            {
                return Results.NotFound();
            }

            // Honeypot filled: pretend success without storing anything.
            if (!string.IsNullOrWhiteSpace(body.Website))
            {
                return Results.Accepted(value: new CommentCreated(Guid.NewGuid(), nameof(CommentStatus.Pending)));
            }

            var comment = new Comment
            {
                VideoId = id,
                Author = body.Author!.Trim(),
                Text = body.Text!.Trim(),
                Status = CommentStatus.Pending
            };
            db.Comments.Add(comment);
            await db.SaveChangesAsync(ct);
            return Results.Accepted(value: new CommentCreated(comment.Id, nameof(CommentStatus.Pending)));
        }).RequireRateLimiting(RateLimitPolicies.PublicWrites);
    }
}
