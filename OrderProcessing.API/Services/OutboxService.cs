using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OrderProcessing.API.Domain;

namespace OrderProcessing.API.Services;

public interface IOutboxService
{
    void AddMessage<T>(T message) where T : class;
    Task<List<OutboxMessage>> FetchMessages(int batchSize, CancellationToken ct);
    Task DeleteMessages(List<OutboxMessage> messages, CancellationToken ct);
    DatabaseFacade Database { get; }
}

[RegisterScoped<IOutboxService>]
public class OutboxService(AppDbContext db) : IOutboxService
{
    public DatabaseFacade Database { get; } = db.Database;

    public void AddMessage<T>(T message) where T : class
    {
        var outboxMessage = new OutboxMessage
        {
            MessageType = typeof(T).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(message)
        };

        db.OutboxMessages.Add(outboxMessage);
    }

    [SuppressMessage("Security", "EF1002:Risk of vulnerability to SQL injection.")]
    public async Task<List<OutboxMessage>> FetchMessages(int batchSize, CancellationToken ct)
    {
        return await db.OutboxMessages
            .FromSqlRaw( // lang=PostgreSQL
                $$"""
                  SELECT * FROM "{{nameof(OutboxMessage)}}"
                  ORDER BY "{{nameof(OutboxMessage.Id)}}"
                  LIMIT {0}
                  FOR UPDATE SKIP LOCKED
                  """,
                batchSize)
            .ToListAsync(ct);
    }

    public async Task DeleteMessages(List<OutboxMessage> messages, CancellationToken ct)
    {
        var messageIds = messages.Select(x => x.Id);
        await db.OutboxMessages
            .Where(m => messageIds.Contains(m.Id))
            .ExecuteDeleteAsync(ct);
    }
}