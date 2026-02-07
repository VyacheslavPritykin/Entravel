using OrderProcessing.API.Domain;
using OrderProcessing.API.Observability;
using OrderProcessing.API.Services;

namespace OrderProcessing.API.Handlers;

[RegisterScoped]
public class ProcessOrderHandler(
    AppDbContext db,
    IInventoryService inventoryService,
    IDiscountService discountService,
    IOrderMetrics metrics,
    TimeProvider timeProvider,
    ILogger<ProcessOrderHandler> logger)
{
    public async Task Handle(long orderId, CancellationToken ct = default)
    {
        var order = await db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == orderId, ct);
        if (order.IsTerminalStatus())
        {
            logger.LogInformation("Order is already in terminal state {Status}, skipping", order.Status);
            return;
        }

        try
        {
            await inventoryService.Validate(order, ct);
            await discountService.ApplyDiscount(order, ct);

            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tran = await db.Database.BeginTransactionAsync(ct);
                await inventoryService.DecrementStock(order);

                order.MarkAsProcessed(timeProvider.GetUtcNow().UtcDateTime);

                await db.SaveChangesAsync(ct);
                await tran.CommitAsync(ct);
            });

            logger.LogInformation("Order processed. Final amount: {TotalAmount}", order.TotalAmount);
            metrics.OrderProcessed();
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            
            order = await db.Orders.FirstAsync(o => o.Id == orderId, ct);
            if (order.Status == OrderStatus.Created)
            {
                logger.LogError(ex, "Order processing failed: {Error}", ex.Message);
                order.MarkAsFailed(ex.Message, timeProvider.GetUtcNow().UtcDateTime);
                await db.SaveChangesAsync(ct);
                metrics.OrderFailed();
            }
            else
            {
                logger.LogInformation("Order was already processed, status is now {Status}", order.Status);
            }
        }
    }
}
