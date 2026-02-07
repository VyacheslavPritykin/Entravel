namespace OrderProcessing.API.Handlers;

[RegisterScoped]
public class GetOrderHandler(AppDbContext db)
{
    public async Task<GetOrderResponse?> Handle(long id, CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.InventoryItem)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null ? null : GetOrderResponse.FromEntity(order);
    }
}
