using MassTransit;
using OrderProcessing.API.Handlers;
using OrderProcessing.API.Messages;

namespace OrderProcessing.API.MessageConsumers;

public class OrderCreatedConsumer(ProcessOrderHandler processOrder, ILogger<OrderCreatedConsumer> logger)
    : IConsumer<OrderCreatedMessage>
{
    public async Task Consume(ConsumeContext<OrderCreatedMessage> context)
    {
        var orderId = context.Message.OrderId;
        using var _ = logger.BeginPropertyScope("OrderId", orderId);
        logger.LogInformation("Received OrderCreated message for order {OrderId}", orderId);
        await processOrder.Handle(orderId, context.CancellationToken);
    }
}