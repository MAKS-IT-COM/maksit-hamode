using MaksIT.HAMode.Abstractions;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MaksIT.HAMode.PostgreSql;

/// <summary>
/// PostgreSQL row lease implementation over public.app_runtime_leases.
/// </summary>
public sealed class RuntimeLeaseServiceNpgsql(
  IRuntimeLeaseConnectionStringProvider connectionStringProvider,
  ILogger<RuntimeLeaseServiceNpgsql> logger,
  NpgsqlDataSource? dataSource = null
) : IRuntimeLeaseService {
  public async Task<Result<bool>> TryAcquireAsync(string leaseName, string holderId, TimeSpan ttl, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(leaseName))
      return Result<bool>.BadRequest(false, "leaseName is required.");

    if (string.IsNullOrWhiteSpace(holderId))
      return Result<bool>.BadRequest(false, "holderId is required.");

    if (ttl <= TimeSpan.Zero)
      return Result<bool>.BadRequest(false, "ttl must be positive.");
    if (dataSource is null && string.IsNullOrWhiteSpace(connectionStringProvider.ConnectionString))
      return Result<bool>.BadRequest(false, "connection string is required.");

    if (string.IsNullOrWhiteSpace(connectionStringProvider.Schema))
      return Result<bool>.BadRequest(false, "schema is required.");
      
    if (string.IsNullOrWhiteSpace(connectionStringProvider.Table))
      return Result<bool>.BadRequest(false, "table is required.");

    try {
      await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

      var acquiredAt = DateTimeOffset.UtcNow;
      var expiresAt = acquiredAt.Add(ttl);
      var tableReference = GetQualifiedTableReference();

      await using var cmd = new NpgsqlCommand(
        $"""
        INSERT INTO {tableReference} (lease_name, holder_id, version, acquired_at_utc, expires_at_utc)
        VALUES (@name, @holder, 1, @acquired, @expires)
        ON CONFLICT (lease_name) DO UPDATE
        SET holder_id = EXCLUDED.holder_id,
            version = {tableReference}.version + 1,
            acquired_at_utc = EXCLUDED.acquired_at_utc,
            expires_at_utc = EXCLUDED.expires_at_utc
        WHERE {tableReference}.expires_at_utc < EXCLUDED.acquired_at_utc
           OR {tableReference}.holder_id = EXCLUDED.holder_id
        RETURNING holder_id;
        """,
        conn);

      cmd.Parameters.AddWithValue("name", leaseName);
      cmd.Parameters.AddWithValue("holder", holderId);
      cmd.Parameters.AddWithValue("acquired", acquiredAt);
      cmd.Parameters.AddWithValue("expires", expiresAt);

      await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
      if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        return Result<bool>.Ok(false);

      var winner = reader.GetString(0);
      return Result<bool>.Ok(string.Equals(winner, holderId, StringComparison.Ordinal));
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable) {
      var qualifiedTableName = $"{connectionStringProvider.Schema}.{connectionStringProvider.Table}";
      logger.LogError(ex, "Lease table {TableName} was not found while acquiring lease {LeaseName}", qualifiedTableName, leaseName);
      return Result<bool>.InternalServerError(false, [
        $"Lease table '{qualifiedTableName}' was not found.",
        "Create the table or set Schema/Table in the PostgreSQL connection provider."
      ]);
    }
    catch (Exception ex) {
      logger.LogError(ex, "TryAcquire lease failed for {LeaseName}", leaseName);
      return Result<bool>.InternalServerError(false, ["Lease acquire failed.", ex.Message]);
    }
  }

  public async Task<Result> ReleaseAsync(string leaseName, string holderId, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(leaseName))
      return Result.BadRequest("leaseName is required.");

    if (string.IsNullOrWhiteSpace(holderId))
      return Result.BadRequest("holderId is required.");

    if (dataSource is null && string.IsNullOrWhiteSpace(connectionStringProvider.ConnectionString))
      return Result.BadRequest("connection string is required.");

    if (string.IsNullOrWhiteSpace(connectionStringProvider.Schema))
      return Result.BadRequest("schema is required.");

    if (string.IsNullOrWhiteSpace(connectionStringProvider.Table))
      return Result.BadRequest("table is required.");

    try {
      await using var conn = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
      var tableReference = GetQualifiedTableReference();

      await using var cmd = new NpgsqlCommand(
        $"""
        DELETE FROM {tableReference}
        WHERE lease_name = @name AND holder_id = @holder;
        """,
        conn);

      cmd.Parameters.AddWithValue("name", leaseName);
      cmd.Parameters.AddWithValue("holder", holderId);
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
      return Result.Ok();
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable) {
      var qualifiedTableName = $"{connectionStringProvider.Schema}.{connectionStringProvider.Table}";
      logger.LogWarning(ex, "Lease table {TableName} was not found while releasing lease {LeaseName}", qualifiedTableName, leaseName);
      return Result.InternalServerError([
        $"Lease table '{qualifiedTableName}' was not found.",
        "Create the table or set Schema/Table in the PostgreSQL connection provider."
      ]);
    }
    catch (Exception ex) {
      logger.LogWarning(ex, "Release lease failed for {LeaseName} (ignored).", leaseName);
      return Result.InternalServerError(["Lease release failed.", ex.Message]);
    }
  }

  private string GetQualifiedTableReference() =>
    $"{QuoteIdentifier(connectionStringProvider.Schema)}.{QuoteIdentifier(connectionStringProvider.Table)}";

  private static string QuoteIdentifier(string identifier) =>
    $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

  private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken) {
    if (dataSource is not null)
      return await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

    var connection = new NpgsqlConnection(connectionStringProvider.ConnectionString);
    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
    return connection;
  }
}
