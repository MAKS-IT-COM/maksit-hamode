using MaksIT.HAMode.Abstractions;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MaksIT.HAMode.Redis;

/// <summary>
/// Redis runtime lease implementation using atomic Lua scripts and key TTL.
/// </summary>
public sealed class RuntimeLeaseServiceRedis(
  IRuntimeLeaseRedisConnectionProvider connectionProvider,
  ILogger<RuntimeLeaseServiceRedis> logger
) : IRuntimeLeaseService, IAsyncDisposable {
  private static readonly LuaScript AcquireScript = LuaScript.Prepare(
    """
    local current = redis.call('GET', @key)
    if (not current) or (current == @holder) then
      redis.call('PSETEX', @key, @ttlMs, @holder)
      return 1
    end
    return 0
    """);

  private static readonly LuaScript ReleaseScript = LuaScript.Prepare(
    """
    local current = redis.call('GET', @key)
    if current == @holder then
      redis.call('DEL', @key)
      return 1
    end
    return 0
    """);

  private readonly SemaphoreSlim _connectionLock = new(1, 1);
  private ConnectionMultiplexer? _multiplexer;

  public async Task<Result<bool>> TryAcquireAsync(string leaseName, string holderId, TimeSpan ttl, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(leaseName))
      return Result<bool>.BadRequest(false, "leaseName is required.");
    if (string.IsNullOrWhiteSpace(holderId))
      return Result<bool>.BadRequest(false, "holderId is required.");
    if (ttl <= TimeSpan.Zero)
      return Result<bool>.BadRequest(false, "ttl must be positive.");

    try {
      var db = (await GetDatabaseAsync(cancellationToken).ConfigureAwait(false)).Database;
      var key = BuildKey(leaseName);
      var ttlMs = Convert.ToInt64(Math.Ceiling(ttl.TotalMilliseconds));
      var result = (long)await db.ScriptEvaluateAsync(
        AcquireScript,
        new { key, holder = holderId, ttlMs }).ConfigureAwait(false);

      return Result<bool>.Ok(result == 1);
    }
    catch (Exception ex) {
      logger.LogError(ex, "Redis TryAcquire lease failed for {LeaseName}", leaseName);
      return Result<bool>.InternalServerError(false, ["Lease acquire failed.", ex.Message]);
    }
  }

  public async Task<Result> ReleaseAsync(string leaseName, string holderId, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(leaseName))
      return Result.BadRequest("leaseName is required.");
    if (string.IsNullOrWhiteSpace(holderId))
      return Result.BadRequest("holderId is required.");

    try {
      var db = (await GetDatabaseAsync(cancellationToken).ConfigureAwait(false)).Database;
      var key = BuildKey(leaseName);
      _ = await db.ScriptEvaluateAsync(ReleaseScript, new { key, holder = holderId }).ConfigureAwait(false);
      return Result.Ok();
    }
    catch (Exception ex) {
      logger.LogWarning(ex, "Redis Release lease failed for {LeaseName} (ignored).", leaseName);
      return Result.InternalServerError(["Lease release failed.", ex.Message]);
    }
  }

  private string BuildKey(string leaseName) => $"{connectionProvider.KeyPrefix}{leaseName}";

  private async Task<(IConnectionMultiplexer Multiplexer, IDatabase Database)> GetDatabaseAsync(CancellationToken cancellationToken) {
    if (_multiplexer is { IsConnected: true } connected)
      return (connected, connected.GetDatabase());

    await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
    try {
      if (_multiplexer is not { IsConnected: true }) {
        _multiplexer?.Dispose();
        _multiplexer = await ConnectionMultiplexer.ConnectAsync(connectionProvider.Configuration).ConfigureAwait(false);
      }

      return (_multiplexer, _multiplexer.GetDatabase());
    }
    finally {
      _connectionLock.Release();
    }
  }

  public async ValueTask DisposeAsync() {
    await _connectionLock.WaitAsync().ConfigureAwait(false);
    try {
      if (_multiplexer is not null) {
        await _multiplexer.CloseAsync(allowCommandsToComplete: false).ConfigureAwait(false);
        _multiplexer.Dispose();
        _multiplexer = null;
      }
    }
    finally {
      _connectionLock.Release();
      _connectionLock.Dispose();
    }
  }
}
