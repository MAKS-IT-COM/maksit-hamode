namespace MaksIT.HAMode.Abstractions;

/// <summary>
/// Supplies Redis connection settings for runtime lease persistence.
/// </summary>
public interface IRuntimeLeaseRedisConnectionProvider : IRuntimeLeaseConnectionProvider {
  /// <summary>StackExchange.Redis configuration string.</summary>
  string Configuration { get; }

  /// <summary>Optional key prefix to isolate lease keys per application.</summary>
  string KeyPrefix => "app_runtime_leases:";
}
