using Microsoft.EntityFrameworkCore;
using VideoHub.Api.Contracts;
using VideoHub.Api.Services;
using VideoHub.Data;

namespace VideoHub.Api.Endpoints;

public static class AdminVideoEndpoints
{
    public static void MapAdminVideoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/videos").RequireAuthorization(AdminSeeder.AdminRole);

        group.MapPost("/", async (VideoRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var errors = Validation.Video(body.Title, body.Description, body.Url);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var video = new Video
            {
                Title = body.Title!.Trim(),
                Description = body.Description?.Trim() ?? "",
                Url = body.Url!.Trim()
            };
            db.Videos.Add(video);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/videos/{video.Id}", await Load(db, video.Id, ct));
        });

        group.MapPut("/{id:guid}", async (Guid id, VideoRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var errors = Validation.Video(body.Title, body.Description, body.Url);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var video = await db.Videos.FindAsync([id], ct);
            if (video is null)
            {
                return Results.NotFound();
            }

            video.Title = body.Title!.Trim();
            video.Description = body.Description?.Trim() ?? "";
            video.Url = body.Url!.Trim();
            video.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(await Load(db, id, ct));
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var deleted = await db.Videos.Where(v => v.Id == id).ExecuteDeleteAsync(ct);
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });
    }

    private static async Task<VideoSummary> Load(AppDbContext db, Guid id, CancellationToken ct) =>
        (await db.Videos.AsNoTracking().Where(v => v.Id == id).WithRatings().FirstAsync(ct)).ToSummary();
}
