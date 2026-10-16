using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace VideoHub.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Video>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(Limits.Title).IsRequired();
            e.Property(x => x.Description).HasMaxLength(Limits.Description).IsRequired();
            e.Property(x => x.Url).HasMaxLength(Limits.Url).IsRequired();
            e.HasIndex(x => x.CreatedAt);
        });

        b.Entity<Rating>(e =>
        {
            e.Property(x => x.VoterHash).HasMaxLength(Limits.Hash).IsRequired();
            e.Property(x => x.IpHash).HasMaxLength(Limits.Hash).IsRequired();
            e.ToTable(t => t.HasCheckConstraint("CK_Ratings_Stars", "\"Stars\" BETWEEN 1 AND 5"));
            e.HasOne(x => x.Video).WithMany(v => v.Ratings).HasForeignKey(x => x.VideoId).OnDelete(DeleteBehavior.Cascade);
            // One vote per video per anonymous voter (cookie) and per client IP.
            e.HasIndex(x => new { x.VideoId, x.VoterHash }).IsUnique();
            e.HasIndex(x => new { x.VideoId, x.IpHash }).IsUnique();
        });

        b.Entity<Comment>(e =>
        {
            e.Property(x => x.Author).HasMaxLength(Limits.Author).IsRequired();
            e.Property(x => x.Text).HasMaxLength(Limits.CommentText).IsRequired();
            e.Property(x => x.ModeratedBy).HasMaxLength(256);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            e.HasOne(x => x.Video).WithMany(v => v.Comments).HasForeignKey(x => x.VideoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.VideoId, x.Status, x.CreatedAt });
        });
    }
}
