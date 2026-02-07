using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OrderProcessing.API.Handlers;

namespace OrderProcessing.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    [HttpPost]
    public async Task<Results<Accepted<CreateOrderResponse>, Ok<CreateOrderResponse>, BadRequest>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        [FromServices] CreateOrderHandler createOrder) =>
        await createOrder.Handle(request) switch
        {
            CreateOrderResult.OrderCreated r =>
                TypedResults.Accepted((string?)null, new CreateOrderResponse(r.OrderId, r.Status)),
            CreateOrderResult.OrderAlreadyExists r => TypedResults.Ok(new CreateOrderResponse(r.OrderId, r.Status)),
            CreateOrderResult.InvalidRequest => TypedResults.BadRequest(),
            _ => throw new InvalidOperationException("Unexpected result type")
        };

    [HttpGet("{id:long}")]
    public async Task<Results<Ok<GetOrderResponse>, NotFound>> GetOrder(
        long id,
        [FromServices] GetOrderHandler getOrder,
        CancellationToken ct)
    {
        var order = await getOrder.Handle(id, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order);
    }
}