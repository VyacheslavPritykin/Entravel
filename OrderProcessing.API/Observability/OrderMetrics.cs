using System.Diagnostics.Metrics;

namespace OrderProcessing.API.Observability;

public interface IOrderMetrics
{
    void OrderCreated();
    void OrderProcessed();
    void OrderFailed();
}

[RegisterSingleton<IOrderMetrics>]
public sealed class OrderMetrics(IMeterFactory meterFactory) : IOrderMetrics
{
    public const string MeterName = "OrderProcessing";
    
    private readonly Counter<long> _ordersCreated = meterFactory.Create(MeterName)
        .CreateCounter<long>("orders.created", "{orders}", "Orders created");

    private readonly Counter<long> _ordersProcessed = meterFactory.Create(MeterName)
        .CreateCounter<long>("orders.processed", "{orders}", "Orders successfully processed");

    private readonly Counter<long> _ordersFailed = meterFactory.Create(MeterName)
        .CreateCounter<long>("orders.failed", "{orders}", "Orders that failed processing");
    
    public void OrderCreated() => _ordersCreated.Add(1);
    public void OrderProcessed() => _ordersProcessed.Add(1);
    public void OrderFailed() => _ordersFailed.Add(1);
}
