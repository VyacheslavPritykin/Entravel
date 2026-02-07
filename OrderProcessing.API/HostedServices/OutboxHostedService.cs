using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Options;
using OrderProcessing.API.Domain;
using OrderProcessing.API.Services;

namespace OrderProcessing.API.HostedServices;

public class OutboxHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxHostedService> logger,
    IOptionsMonitor<OutboxOptions> outboxOptions) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("OutboxProcessor started");
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var batchSize = outboxOptions.CurrentValue.BatchSize;
                while (await ProcessPendingMessagesAsync(batchSize, ct) == batchSize)
                {
                    batchSize = outboxOptions.CurrentValue.BatchSize;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error processing outbox messages");
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                continue;
            }

            var pollingInterval = outboxOptions.CurrentValue.PollingInterval;
            logger.LogDebug("Waiting for {PollingInterval} before checking for new outbox messages", pollingInterval);
            await Task.Delay(pollingInterval, ct);
        }

        logger.LogInformation("OutboxProcessor stopped");
    }

    private async Task<int> ProcessPendingMessagesAsync(int batchSize, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxService>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var strategy = outboxRepository.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tran = await outboxRepository.Database.BeginTransactionAsync(ct);
            var messages = await outboxRepository.FetchMessages(batchSize, ct);
            foreach (var message in messages)
                await ProcessMessage(publishEndpoint, message, ct);

            await outboxRepository.DeleteMessages(messages, ct);
            await tran.CommitAsync(ct);

            return messages.Count;
        });
    }

    private async Task ProcessMessage(
        IPublishEndpoint publishEndpoint,
        OutboxMessage message,
        CancellationToken ct)
    {
        try
        {
            var messageType = Type.GetType(message.MessageType)!;
            var payload = JsonSerializer.Deserialize(message.Payload, messageType)!;

            await publishEndpoint.Publish(payload, messageType, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish outbox message {@Message}", message);
            throw;
        }
    }
}

public class OutboxOptions
{
    public required TimeSpan PollingInterval { get; set; }
    public required int BatchSize { get; set; }
}