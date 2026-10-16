namespace VideoHub.Data;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VideoId { get; set; }
    public Video? Video { get; set; }
    public string Author { get; set; } = "";
    public string Text { get; set; } = "";
    public CommentStatus Status { get; set; } = CommentStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModeratedAt { get; set; }
    public string? ModeratedBy { get; set; }
}
