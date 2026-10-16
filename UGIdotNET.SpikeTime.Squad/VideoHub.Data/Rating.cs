namespace VideoHub.Data;

public class Rating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VideoId { get; set; }
    public Video? Video { get; set; }
    public int Stars { get; set; }
    // Salted SHA-256 hex of the anonymous voter id (cookie) and of the client IP.
    public string VoterHash { get; set; } = "";
    public string IpHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
