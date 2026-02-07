namespace OrderProcessing.API.Domain;

public class OutboxMessage : IEntity
{
    public long Id { get; set; }
    [MaxLength(512)] public required string MessageType { get; set; }
    public required string Payload { get; set; }
    public DateTime CreatedAt { get; set; }
}
