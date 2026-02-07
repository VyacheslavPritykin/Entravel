using OrderProcessing.API.Domain;

namespace OrderProcessing.API.Services;

public interface IDiscountService
{
    Task ApplyDiscount(Order order, CancellationToken ct);
}

[RegisterSingleton<IDiscountService>]
public class DiscountService(ILogger<DiscountService> logger) : IDiscountService
{
    public async Task ApplyDiscount(Order order, CancellationToken ct)
    {
        logger.LogInformation("Calculating discount");

        // simulate processing
        await Task.Delay(100, ct);
        
        if (order.TotalAmount <= 100m)
        {
            logger.LogInformation("No discount applicable for order {OrderId}", order.Id);
            return;
        }

        var discount = Math.Round(order.TotalAmount * 0.10m, 2);
        order.TotalAmount -= discount;
        
        logger.LogInformation("Discount: {Discount}, new TotalAmount: {TotalAmount}", discount, order.TotalAmount);
    }
}