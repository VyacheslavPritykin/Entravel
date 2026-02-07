using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderProcessing.API.Domain;

public class OrderItem : IEntityTypeConfiguration<OrderItem>
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long InventoryItemId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    public Order? Order { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasOne(e => e.InventoryItem)
            .WithMany()
            .HasForeignKey(e => e.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.OrderId, e.InventoryItemId })
            .IsUnique();
    }
}