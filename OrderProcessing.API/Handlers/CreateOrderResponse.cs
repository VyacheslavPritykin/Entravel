using OrderProcessing.API.Domain;

namespace OrderProcessing.API.Handlers;

public record CreateOrderResponse(long OrderId, OrderStatus Status);
