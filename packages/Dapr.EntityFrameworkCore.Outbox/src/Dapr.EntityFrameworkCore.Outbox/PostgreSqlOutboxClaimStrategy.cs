// ------------------------------------------------------------------------
// Copyright 2026 The Dapr Authors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ------------------------------------------------------------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dapr.EntityFrameworkCore.Outbox;

/// <summary>
/// PostgreSQL-optimized claim strategy that uses <c>SELECT ... FOR UPDATE SKIP LOCKED</c>
/// so multiple dispatcher replicas can drain the outbox concurrently without blocking.
/// The claim happens inside a transaction; the row updates are committed before returning.
/// </summary>
public sealed class PostgreSqlOutboxClaimStrategy : IOutboxClaimStrategy
{
    private readonly RelationalOutboxClaimStrategy releaseFallback = new();

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> ClaimBatchAsync(
        DbContext dbContext,
        DaprOutboxOptions options,
        string lockOwner,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrEmpty(lockOwner);

        var cols = ResolveColumns(dbContext);
        var quotedTable = cols.QualifiedTable;
        var lockedUntil = now.Add(options.LockDuration);

        var sql =
            $"WITH candidate AS (\n" +
            $"    SELECT {cols.Id}\n" +
            $"    FROM {quotedTable}\n" +
            $"    WHERE {cols.ProcessedAt} IS NULL\n" +
            $"      AND {cols.AttemptCount} < @maxAttempts\n" +
            $"      AND ({cols.LockedUntil} IS NULL OR {cols.LockedUntil} < @now)\n" +
            $"    ORDER BY {cols.OccurredAt}\n" +
            $"    LIMIT @batchSize\n" +
            $"    FOR UPDATE SKIP LOCKED\n" +
            $")\n" +
            $"UPDATE {quotedTable} AS t\n" +
            $"SET {cols.LockOwner} = @owner,\n" +
            $"    {cols.LockedUntil} = @lockedUntil,\n" +
            $"    {cols.AttemptCount} = t.{cols.AttemptCount} + 1\n" +
            $"FROM candidate c\n" +
            $"WHERE t.{cols.Id} = c.{cols.Id}\n" +
            $"RETURNING t.*;";

        var conn = dbContext.Database.GetDbConnection();
        var opened = false;
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            opened = true;
        }

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;

            AddParameter(cmd, "@batchSize", options.BatchSize);
            AddParameter(cmd, "@owner", lockOwner);
            AddParameter(cmd, "@lockedUntil", lockedUntil);
            AddParameter(cmd, "@maxAttempts", options.MaxAttempts);
            AddParameter(cmd, "@now", now);

            var claimed = new List<OutboxMessage>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                claimed.Add(Materialize(reader, cols));
            }

            return claimed;
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    public Task ReleaseAsync(
        DbContext dbContext,
        IReadOnlyList<OutboxDispatchResult> results,
        string lockOwner,
        CancellationToken cancellationToken)
        => releaseFallback.ReleaseAsync(dbContext, results, lockOwner, cancellationToken);

    private sealed class ColumnMap
    {
        public required string QualifiedTable { get; init; }
        public required string Id { get; init; }
        public required string SchemaVersion { get; init; }
        public required string OccurredAt { get; init; }
        public required string PubSubName { get; init; }
        public required string Topic { get; init; }
        public required string ContentType { get; init; }
        public required string Payload { get; init; }
        public required string MetadataJson { get; init; }
        public required string CorrelationId { get; init; }
        public required string ProcessedAt { get; init; }
        public required string AttemptCount { get; init; }
        public required string LastError { get; init; }
        public required string LockOwner { get; init; }
        public required string LockedUntil { get; init; }
    }

    private static ColumnMap ResolveColumns(DbContext dbContext)
    {
        var entityType = dbContext.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException(
                "OutboxMessage is not registered in the EF Core model. Call AddDaprOutbox() in OnModelCreating.");

        var schema = entityType.GetSchema();
        var table = entityType.GetTableName()
            ?? throw new InvalidOperationException("OutboxMessage has no table name configured.");

        var storeObject = StoreObjectIdentifier.Table(table, schema);

        var qualifiedTable = string.IsNullOrEmpty(schema)
            ? $"\"{table}\""
            : $"\"{schema}\".\"{table}\"";

        return new ColumnMap
        {
            QualifiedTable = qualifiedTable,
            Id = Quote(GetColumn(entityType, nameof(OutboxMessage.Id), storeObject)),
            SchemaVersion = Quote(GetColumn(entityType, nameof(OutboxMessage.SchemaVersion), storeObject)),
            OccurredAt = Quote(GetColumn(entityType, nameof(OutboxMessage.OccurredAt), storeObject)),
            PubSubName = Quote(GetColumn(entityType, nameof(OutboxMessage.PubSubName), storeObject)),
            Topic = Quote(GetColumn(entityType, nameof(OutboxMessage.Topic), storeObject)),
            ContentType = Quote(GetColumn(entityType, nameof(OutboxMessage.ContentType), storeObject)),
            Payload = Quote(GetColumn(entityType, nameof(OutboxMessage.Payload), storeObject)),
            MetadataJson = Quote(GetColumn(entityType, nameof(OutboxMessage.MetadataJson), storeObject)),
            CorrelationId = Quote(GetColumn(entityType, nameof(OutboxMessage.CorrelationId), storeObject)),
            ProcessedAt = Quote(GetColumn(entityType, nameof(OutboxMessage.ProcessedAt), storeObject)),
            AttemptCount = Quote(GetColumn(entityType, nameof(OutboxMessage.AttemptCount), storeObject)),
            LastError = Quote(GetColumn(entityType, nameof(OutboxMessage.LastError), storeObject)),
            LockOwner = Quote(GetColumn(entityType, nameof(OutboxMessage.LockOwner), storeObject)),
            LockedUntil = Quote(GetColumn(entityType, nameof(OutboxMessage.LockedUntil), storeObject)),
        };
    }

    private static string GetColumn(IEntityType entityType, string propertyName, StoreObjectIdentifier storeObject)
    {
        var property = entityType.FindProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on OutboxMessage.");
        return property.GetColumnName(storeObject)
            ?? throw new InvalidOperationException($"Column name for '{propertyName}' could not be resolved.");
    }

    private static string Quote(string identifier) => $"\"{identifier}\"";

    private static void AddParameter(DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    private static OutboxMessage Materialize(DbDataReader reader, ColumnMap cols)
    {
        return new OutboxMessage
        {
            Id = reader.GetGuid(reader.GetOrdinal(Unquote(cols.Id))),
            SchemaVersion = reader.GetInt32(reader.GetOrdinal(Unquote(cols.SchemaVersion))),
            OccurredAt = ReadDateTimeOffset(reader, Unquote(cols.OccurredAt)),
            PubSubName = reader.GetString(reader.GetOrdinal(Unquote(cols.PubSubName))),
            Topic = reader.GetString(reader.GetOrdinal(Unquote(cols.Topic))),
            ContentType = reader.GetString(reader.GetOrdinal(Unquote(cols.ContentType))),
            Payload = (byte[])reader[Unquote(cols.Payload)],
            MetadataJson = ReadNullableString(reader, Unquote(cols.MetadataJson)),
            CorrelationId = ReadNullableString(reader, Unquote(cols.CorrelationId)),
            ProcessedAt = ReadNullableDateTimeOffset(reader, Unquote(cols.ProcessedAt)),
            AttemptCount = reader.GetInt32(reader.GetOrdinal(Unquote(cols.AttemptCount))),
            LastError = ReadNullableString(reader, Unquote(cols.LastError)),
            LockOwner = ReadNullableString(reader, Unquote(cols.LockOwner)),
            LockedUntil = ReadNullableDateTimeOffset(reader, Unquote(cols.LockedUntil)),
        };
    }

    private static string Unquote(string quoted) => quoted.Trim('"');

    private static DateTimeOffset ReadDateTimeOffset(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc), TimeSpan.Zero),
            _ => throw new InvalidOperationException($"Unexpected type for column {column}: {value.GetType()}"),
        };
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : ReadDateTimeOffset(reader, column);
    }

    private static string? ReadNullableString(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
