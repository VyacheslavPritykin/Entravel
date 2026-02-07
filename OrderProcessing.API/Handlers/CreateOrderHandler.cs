using OrderProcessing.API.Domain;
using OrderProcessing.API.Messages;
using OrderProcessing.API.Observability;
using OrderProcessing.API.Services;

namespace OrderProcessing.API.Handlers;

[RegisterScoped]
public class CreateOrderHandler(
    AppDbContext db,
    IOutboxService outboxService,
    IOrderMetrics orderMetrics,
    ILogger<CreateOrderHandler> logger)
{
    public async Task<CreateOrderResult> Handle(CreateOrderRequest request)
    {
        using var _ = logger.BeginPropertyScope(
            ("IdempotencyKey", request.IdempotencyKey),
            ("CustomerId", request.CustomerId));

        var order = request.ToOrderEntity();

        var strategy = db.Database.CreateExecutionStrategy();
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                
                // save order first to get the id
                db.Orders.Add(order);
                await db.SaveChangesAsync();
                
                outboxService.AddMessage(new OrderCreatedMessage(order.Id));
                await db.SaveChangesAsync();
                
                await transaction.CommitAsync();
            });
        }
        catch (DbUpdateException e) when (e.IsForeignKeyViolation())
        {
            logger.LogWarning("Foreign key violation for order: {@Order}", request);
            return CreateOrderResult.Invalid();
        }
        catch (DbUpdateException e) when (e.IsUniqueKeyViolation())
        {
            var existingOrder = await db.Orders
                .Where(o => o.IdempotencyKey == request.IdempotencyKey)
                .FirstAsync();

            logger.LogInformation("Duplicate order {OrderId}", existingOrder.Id);
            return CreateOrderResult.Duplicate(existingOrder);
        }

        logger.LogInformation("Order {OrderId} created with {ItemCount} items", order.Id, order.Items.Count);
        orderMetrics.OrderCreated();

        return CreateOrderResult.Created(order);
    }
}

public abstract record CreateOrderResult
{
    public sealed record OrderCreated(long OrderId, OrderStatus Status) : CreateOrderResult;
    public sealed record OrderAlreadyExists(long OrderId, OrderStatus Status) : CreateOrderResult;
    public sealed record InvalidRequest : CreateOrderResult;

    public static CreateOrderResult Created(Order order) => new OrderCreated(order.Id, order.Status);
    public static CreateOrderResult Duplicate(Order order) => new OrderAlreadyExists(order.Id, order.Status);
    public static CreateOrderResult Invalid() => new InvalidRequest();
}
