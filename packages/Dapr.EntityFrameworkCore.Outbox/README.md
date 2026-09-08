# Dapr.EntityFrameworkCore.Outbox

A transactional outbox implementation for the Dapr .NET SDK using Entity Framework Core. Enqueues domain events inside the `DbContext` transaction and publishes them reliably via `DaprClient`.

Extracted from [Dapr .NET SDK PR #1863](https://github.com/dapr/dotnet-sdk/pull/1863).

## Installation

Reference the project (or future NuGet package):

```xml
<ProjectReference Include="path/to/Dapr.EntityFrameworkCore.Outbox.csproj" />
```

## Quick Start

### 1. Configure your DbContext

```csharp
public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddDaprOutbox();
    }
}
```

### 2. Register services

```csharp
services.AddDaprOutbox<AppDbContext>()
    .AddDefaultDispatcher()
    .AddPostgreSqlClaimStrategy();  // or AddSqlServerClaimStrategy()
```

### 3. Implement `IHasDomainEvents` on your aggregate roots

```csharp
public abstract class AggregateRoot : IHasDomainEvents
{
    private readonly List<object> _domainEvents = [];

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(object domainEvent) => _domainEvents.Add(domainEvent);
}
```

### 4. Annotate domain events

```csharp
[DaprOutboxEvent("pubsub", "orders.created")]
public sealed record OrderCreatedEvent(Guid OrderId, decimal Total);
```

### 5. Raise events in your domain

```csharp
public sealed class Order : AggregateRoot
{
    public static Order Create(decimal total)
    {
        var order = new Order { Id = Guid.NewGuid(), Total = total };
        order.Raise(new OrderCreatedEvent(order.Id, order.Total));
        return order;
    }
}
```

When `SaveChangesAsync` is called, the interceptor captures domain events from all tracked entities implementing `IHasDomainEvents`, converts them to `OutboxMessage` rows, and commits everything in a single transaction. The background dispatcher then publishes them via Dapr pub/sub.

## Explicit Enqueue

For events not tied to domain entities, use `DbContext.EnqueueOutbox()`:

```csharp
dbContext.EnqueueOutbox(
    pubSubName: "pubsub",
    topic: "notifications.sent",
    payload: new { UserId = userId, Message = "Hello" });

await dbContext.SaveChangesAsync();
```

## Configuration

```csharp
services.AddDaprOutbox<AppDbContext>(options =>
{
    options.TableName = "DaprOutboxMessages";  // default
    options.PollInterval = TimeSpan.FromSeconds(5);
    options.BatchSize = 50;
    options.LockDuration = TimeSpan.FromSeconds(30);
    options.MaxAttempts = 10;
    options.RetentionPeriod = TimeSpan.FromDays(7);  // null = keep forever
    options.HealthCheckThreshold = TimeSpan.FromMinutes(5);
});
```

| Option | Default | Description |
|---|---|---|
| `TableName` | `"DaprOutboxMessages"` | Outbox table name |
| `SchemaName` | `null` | Database schema |
| `PollInterval` | 5s | Idle poll interval |
| `BatchSize` | 50 | Max rows per dispatch cycle |
| `LockDuration` | 30s | Claim lock duration |
| `MaxAttempts` | 10 | Max publish attempts before dead-letter |
| `RetentionPeriod` | `null` | Auto-delete processed rows older than this |
| `HealthCheckThreshold` | `null` | Lag threshold for unhealthy status |
| `ShutdownDrainTimeout` | 30s | Grace period on shutdown |

## Builder Extensions

| Method | Description |
|---|---|
| `AddDefaultDispatcher()` | Registers the polling dispatcher and background service |
| `AddPostgreSqlClaimStrategy()` | `SELECT ... FOR UPDATE SKIP LOCKED` (recommended for PostgreSQL) |
| `AddSqlServerClaimStrategy()` | `WITH (ROWLOCK, UPDLOCK, READPAST)` (recommended for SQL Server) |
| `AddRetentionService()` | Background cleanup of processed messages |
| `AddOutboxHealthCheck()` | ASP.NET Core health check for outbox lag |
| `UseMessageFactory<T>()` | Custom `IOutboxMessageFactory` implementation |
| `UseDispatcher<T>()` | Custom `IOutboxDispatcher` implementation |
| `UseClaimStrategy<T>()` | Custom `IOutboxClaimStrategy` implementation |

## Claim Strategies

| Strategy | Database | Technique |
|---|---|---|
| `RelationalOutboxClaimStrategy` | Any EF Core relational | Serializable transaction (portable fallback) |
| `PostgreSqlOutboxClaimStrategy` | PostgreSQL | `FOR UPDATE SKIP LOCKED` (non-blocking) |
| `SqlServerOutboxClaimStrategy` | SQL Server | `ROWLOCK, UPDLOCK, READPAST` (non-blocking) |

## Health Check

```csharp
services.AddDaprOutbox<AppDbContext>()
    .AddDefaultDispatcher()
    .AddPostgreSqlClaimStrategy()
    .AddOutboxHealthCheck(name: "dapr-outbox", tags: ["ready"]);
```

Returns Healthy/Degraded/Unhealthy based on the age of the oldest unprocessed message relative to `HealthCheckThreshold`.

## Observability

The package emits OpenTelemetry spans under activity source `Dapr.EntityFrameworkCore.Outbox`:
- `outbox.flush` — interceptor capturing domain events during SaveChanges
- `outbox.dispatch` — dispatcher publishing messages via DaprClient

## How It Works

1. **Intercept** — `DaprOutboxSaveChangesInterceptor` walks the EF change tracker for entities implementing `IHasDomainEvents`, serializes each event as an `OutboxMessage` row, and adds it to the same transaction.

2. **Commit** — The database transaction commits both the domain state change and the outbox messages atomically. If the commit fails, nothing is published.

3. **Dispatch** — `DaprOutboxHostedService` runs a background loop. On each tick, `PollingOutboxDispatcher` claims a batch of unprocessed messages (using the configured claim strategy) and publishes each via `DaprClient.PublishByteEventAsync`. Successfully published messages are stamped with `ProcessedAt`.

4. **Recover** — If the process crashes after commit but before dispatch, the locked messages eventually expire (after `LockDuration`) and are reclaimed on the next poll cycle. Messages that fail to publish are retried with exponential backoff up to `MaxAttempts`.
