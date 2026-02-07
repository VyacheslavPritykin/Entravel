using OrderProcessing.API.Domain;

namespace OrderProcessing.API.Services;

public interface IInventoryService
{
    Task Validate(Order order, CancellationToken ct);
    Task DecrementStock(Order order);
}

[RegisterScoped<IInventoryService>]
public class InventoryService(AppDbContext db) : IInventoryService
{
    public async Task Validate(Order order, CancellationToken ct)
    {
        // note: OrderItems have unique inventory items according to initial request validation
        var inventorySummary = await db.OrderItems
            .Where(x => x.OrderId == order.Id && x.InventoryItem!.QuantityAvailable >= x.Quantity)
            .Select(x => new { x.Quantity, x.InventoryItem!.UnitPrice })
            .GroupBy(x => 1)
            .Select(x => new { ProductsCount = x.Count(), TotalAmount = x.Sum(y => y.Quantity * y.UnitPrice) })
            .FirstOrDefaultAsync(ct);
        
        if (inventorySummary == null || inventorySummary.ProductsCount < order.Items.Count)
            throw new InvalidOperationException("Not enough products found in inventory");
        
        if (inventorySummary.TotalAmount != order.TotalAmount)
            throw new InvalidOperationException(
                $"TotalAmount mismatch. Sent: {order.TotalAmount}, actual: {inventorySummary.TotalAmount}");
    }
    
    public async Task DecrementStock(Order order)
    {
        // Fails if QuantityAvailable becomes < 0 thanks to the DB check constraint
        await db.OrderItems
            .Where(x => x.OrderId == order.Id)
            .Select(x => new { x.InventoryItem, x.Quantity })
            .ExecuteUpdateAsync(x => x.SetProperty(
                y => y.InventoryItem!.QuantityAvailable,
                y => y.InventoryItem!.QuantityAvailable - y.Quantity));
    }
}
