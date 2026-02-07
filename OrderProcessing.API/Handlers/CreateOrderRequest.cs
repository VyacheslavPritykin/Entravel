using OrderProcessing.API.Domain;

namespace OrderProcessing.API.Handlers;

public class CreateOrderRequest : IValidatableObject
{
    [Required] public Guid? IdempotencyKey { get; set; }
    [Required] public Guid? CustomerId { get; set; }
    [MinLength(1)] public List<CreateOrderItemDto> Items { get; set; } = [];

    /// <summary>
    /// Client-calculated total. Must match server-calculated total or the order will be rejected.
    /// </summary>
    [Range(0.01, 1_000_000_000)]
    public decimal TotalAmount { get; set; }

    public Order ToOrderEntity() => new()
    {
        IdempotencyKey = IdempotencyKey!.Value,
        CustomerId = CustomerId!.Value,
        TotalAmount = TotalAmount,
        Items = Items.Select(i => new OrderItem
        {
            InventoryItemId = i.InventoryItemId,
            Quantity = i.Quantity,
        }).ToList()
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Items.Count != Items.DistinctBy(x => x.InventoryItemId).Count())
            yield return new("An order cannot contain duplicate items.", [nameof(Items)]);
    }
}

public class CreateOrderItemDto
{
    public long InventoryItemId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
}