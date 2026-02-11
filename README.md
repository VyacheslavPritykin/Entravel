# Order Processing Service (.NET 8)

Async order processing microservice. Accepts orders via HTTP, processes them in the background through RabbitMQ, stores everything in PostgreSQL.

## Task Overview

This is a solution to the Entravel Senior .NET Engineer technical task. For the complete task requirements and specifications, see [TASK.md](TASK.md).

## Run

```bash
docker compose up --build
```

- API: `http://localhost:5062` &nbsp;|&nbsp; Swagger: `http://localhost:5062/swagger`
- RabbitMQ UI: `http://localhost:15673` (`guest`/`guest`) &nbsp;|&nbsp; Seq logs: `http://localhost:5342`

Alternatively, with .NET Aspire: `dotnet run --project Entravel.AppHost`

## API

**Create order** — returns `202 Accepted` immediately; processing happens async.

```bash
curl -X POST http://localhost:5062/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "idempotencyKey": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "customerId": "d3f2a1b0-1234-5678-abcd-ef0123456789",
    "items": [
      { "inventoryItemId": 1, "quantity": 2 },
      { "inventoryItemId": 2, "quantity": 1 }
    ],
    "totalAmount": 109.97
  }'
```

**Get order** — `GET /api/orders/{orderId}` returns current status (`Created` → `Processed` | `Failed`).

### Seed inventory

| Id | Name   | UnitPrice | Stock |
|----|--------|-----------|-------|
| 1  | Gadget | 29.99     | 100   |
| 2  | Widget | 49.99     | 50    |
| 3  | Game   | 15.00     | 200   |
| 4  | Ball   | 75.50     | 1     |

## Design Decisions

- **RabbitMQ + MassTransit** for async processing — durable delivery, consumer abstraction; adds infra dependency.
- **Transactional outbox** — order + message saved atomically, avoids dual-write inconsistency; adds polling delay.
- **PostgreSQL + EF Core** — relational integrity, simple migrations. Inventory seeded via EF `HasData` for demo simplicity.
- **Observability** — Serilog structured logs to Seq; OpenTelemetry counters (`orders.created`, `orders.processed`, `orders.failed`).

## Known Simplifications

Production-readiness items intentionally left out to keep the scope minimal:

- **Public IDs are numeric** — clients should not see sequential DB keys. Add a `Uid` (GUID) column with a unique index to `Order`/`InventoryItem` and use it in all public contracts.
- **No authentication or authorization.**
- **No rate limiting / throttling** on API endpoints.
- **No caching** — every read and validation hits PostgreSQL directly.
- **No customer domain** — `CustomerId` is an opaque GUID with no backing user table.
- **Discount mutates `TotalAmount` in place** — should be stored in a separate `DiscountAmount` column to preserve the original value.
- **`FailureReason` is free text** — add a `FailureKind` enum column for structured monitoring and reporting.
- **Outbox does not carry trace context** — `traceparent`/`tracestate` should be persisted alongside each outbox message and restored before publishing so that the consumer's work appears under the same distributed trace as the originating HTTP request.
