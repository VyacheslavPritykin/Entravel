using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderProcessing.API.Domain;

public class InventoryItem : IEntity, IEntityTypeConfiguration<InventoryItem>
{
    public long Id { get; set; }

    [MaxLength(256)]
    public required string Name { get; set; }

    [Precision(18, 2)]
    public decimal UnitPrice { get; set; }

    public int QuantityAvailable { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_InventoryItem_QuantityAvailable_NonNegative",
                $"\"{nameof(QuantityAvailable)}\" >= 0");

            t.HasCheckConstraint(
                "CK_InventoryItem_UnitPrice_NonNegative",
                $"\"{nameof(UnitPrice)}\" >= 0");
        });

        builder.HasData(
            new InventoryItem { Id = 1, Name = "Gadget", UnitPrice = 29.99m, QuantityAvailable = 100 },
            new InventoryItem { Id = 2, Name = "Widget", UnitPrice = 49.99m, QuantityAvailable = 50 },
            new InventoryItem { Id = 3, Name = "Game", UnitPrice = 15.00m, QuantityAvailable = 200 },
            new InventoryItem { Id = 4, Name = "Ball", UnitPrice = 75.50m, QuantityAvailable = 1 });
    }
}
