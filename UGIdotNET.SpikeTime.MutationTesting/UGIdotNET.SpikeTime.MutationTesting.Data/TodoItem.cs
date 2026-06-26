namespace UGIdotNET.SpikeTime.MutationTesting.Data;

public class TodoItem
{
    public Guid Id { get; set; }

    public required string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public void MarkAsComplete() => CompletedAt = DateTime.Now;

    public bool IsCompleted => CompletedAt is not null;

    public bool IsOverdue(DateTime now)
    {
        if (IsCompleted)
        {
            return false;
        }

        return (now - CreatedAt).TotalDays > 7;
    }
}
