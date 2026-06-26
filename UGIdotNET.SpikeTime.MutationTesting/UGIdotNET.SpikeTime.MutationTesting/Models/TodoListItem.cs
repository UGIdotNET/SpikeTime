namespace UGIdotNET.SpikeTime.MutationTesting.Models;

public record TodoListItem(Guid Id, string Title, DateTime CreatedAt, DateTime? CompletedAt);