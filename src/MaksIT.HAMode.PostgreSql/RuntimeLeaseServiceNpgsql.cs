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
  ILogger<RuntimeLeaseServiceNpgsql> logger
) : IRuntimeLeaseService {
  public async Task<Result<bool>> TryAcquireAsync(string leaseName, string holderId, TimeSpan ttl, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(leaseName))
      return Result<bool>.BadRequest(false, "leaseName is required.");
    if (string.IsNullOrWhiteSpace(holderId))
      return Result<bool>.BadRequest(false, "holderId is required.");
    if (ttl <= TimeSpan.Zero)
      return Result<bool>.BadRequest(false, "ttl must be positive.");

    try {
      await using var conn = new NpgsqlConnection(connectionStringProvider.ConnectionString);
      await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

      var acquiredAt = DateTimeOffset.UtcNow;
      var expiresAt = acquiredAt.Add(ttl);

      await using var cmd = new NpgsqlCommand(
        """
        INSERT INTO public.app_runtime_leases (lease_name, holder_id, version, acquired_at_utc, expires_at_utc)
        VALUES (@name, @holder, 1, @acquired, @expires)
        ON CONFLICT (lease_name) DO UPDATE
        SET holder_id = EXCLUDED.holder_id,
            version = public.app_runtime_leases.version + 1,
            acquired_at_utc = EXCLUDED.acquired_at_utc,
            expires_at_utc = EXCLUDED.expires_at_utc
        WHERE public.app_runtime_leases.expires_at_utc < EXCLUDED.acquired_at_utc
           OR public.app_runtime_leases.holder_id = EXCLUDED.holder_id
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

    try {
      await using var conn = new NpgsqlConnection(connectionStringProvider.ConnectionString);
      await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

      await using var cmd = new NpgsqlCommand(
        """
        DELETE FROM public.app_runtime_leases
        WHERE lease_name = @name AND holder_id = @holder;
        """,
        conn);

      cmd.Parameters.AddWithValue("name", leaseName);
      cmd.Parameters.AddWithValue("holder", holderId);
      await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
      return Result.Ok();
    }
    catch (Exception ex) {
      logger.LogWarning(ex, "Release lease failed for {LeaseName} (ignored).", leaseName);
      return Result.InternalServerError(["Lease release failed.", ex.Message]);
    }
  }
}
