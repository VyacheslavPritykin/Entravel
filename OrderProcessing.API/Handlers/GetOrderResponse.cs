using OrderProcessing.API.Domain;

namespace OrderProcessing.API.Handlers;

public class GetOrderResponse
{
    public long Id { get; set; }
    public Guid IdempotencyKey { get; set; }
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public List<OrderItemResponse> Items { get; set; } = [];

    public static GetOrderResponse FromEntity(Order order) => new()
    {
        Id = order.Id,
        IdempotencyKey = order.IdempotencyKey,
        CustomerId = order.CustomerId,
        TotalAmount = order.TotalAmount,
        Status = order.Status,
        CreatedAt = order.CreatedAt,
        ProcessedAt = order.ProcessedAt,
        Items = order.Items.Select(i => new OrderItemResponse
        {
            InventoryItemId = i.InventoryItemId,
            ProductName = i.InventoryItem!.Name,
            Quantity = i.Quantity,
            UnitPrice = i.InventoryItem!.UnitPrice
        }).ToList()
    };
}

public class OrderItemResponse
{
    public long InventoryItemId { get; set; }
    public required string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
