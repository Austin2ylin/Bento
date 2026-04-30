namespace Bento.Api.Models;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Type { get; set; } = string.Empty;

    public int AggregateId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public int AttemptCount { get; set; }

    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;

    public string? LastError { get; set; }
}
