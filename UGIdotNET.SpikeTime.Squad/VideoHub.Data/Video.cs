namespace VideoHub.Data;

public class Video
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public List<Rating> Ratings { get; set; } = [];
    public List<Comment> Comments { get; set; } = [];
}
