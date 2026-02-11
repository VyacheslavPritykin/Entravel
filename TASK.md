# Technical Task: Order Processing Service

## Goal
Implement a small microservice in C# (.NET 8) that processes incoming "orders" asynchronously and stores them in a database.

**Note:** The system should have two core entities: inventory, order.

## Requirements

### Core Functionality

1. **Expose an HTTP API endpoint** that allows submitting an order.
   - Each order has at least a CustomerId, Items, and TotalAmount.
   - The request should return immediately, without waiting for order processing to finish.

2. **Orders should be processed asynchronously** via a background worker.
   - The worker should simulate some business logic (e.g., validating, enriching, or calculating discounts).
   - When processing is done, it should mark the order as "processed" in persistent storage.

3. **Order data should be stored in a database** (PostgreSQL preferred, but not required).

### Non-Functional / Infrastructure

- Use Docker to containerize the service.
- Use Redis, RabbitMQ, or another queueing mechanism to manage asynchronous processing — your choice (but justify it).
- Include basic observability: at least one metric or log showing the number of processed orders.

## Expectations

- Deliver a minimal but complete working example (not production-grade).
- Include a short README.md explaining:
  - How to run the service.
  - Design decisions and trade-offs.
  - Any assumptions you made.
- Submit solution as a public GitHub repository link.

## Deadline

Three days after receiving this test task.
