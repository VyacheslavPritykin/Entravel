using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderProcessing.API.Domain;

// NOTE: In a production system, entities would typically have both:
//   - long Id  (primary key — optimized for joins, indexing, and storage)
//   - Guid Uid (public-facing identifier with a unique index — avoids exposing sequential IDs to clients)
// For this demo only long Id is used for simplicity.
public class Order : IEntity, IEntityTypeConfiguration<Order>
{
    public long Id { get; set; }
    public Guid IdempotencyKey { get; set; }
    public Guid CustomerId { get; set; }
    [Precision(18, 2)] public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; private set; }
    [MaxLength(1024)] public string? FailureReason { get; set; }
    public List<OrderItem> Items { get; set; } = [];
    [Timestamp] public uint RowVersion { get; set; }
    
    public bool IsTerminalStatus() => Status is OrderStatus.Processed or OrderStatus.Failed;
    
    public void MarkAsProcessed(DateTime processedAt)
    {
        Status = OrderStatus.Processed;
        ProcessedAt = processedAt;
    }

    public void MarkAsFailed(string reason, DateTime processedAt)
    {
        Status = OrderStatus.Failed;
        FailureReason = reason;
        ProcessedAt = processedAt;
    }

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasIndex(e => e.IdempotencyKey)
            .IsUnique();

        builder.HasMany(e => e.Items)
            .WithOne(e => e.Order)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public enum OrderStatus
{
    Created = 0,
    Processed = 1,
    Failed = 2
}