namespace MaksIT.HAMode.Abstractions;

/// <summary>
/// Supplies etcd connection settings for runtime lease persistence.
/// </summary>
public interface IRuntimeLeaseEtcdConnectionProvider : IRuntimeLeaseConnectionProvider {
  /// <summary>Comma-separated etcd endpoint list (for example "http://localhost:2379").</summary>
  string Endpoints { get; }

  /// <summary>Optional etcd username for authenticated clusters.</summary>
  string? Username => null;

  /// <summary>Optional etcd password for authenticated clusters.</summary>
  string? Password => null;

  /// <summary>Optional key prefix to isolate lease keys per application.</summary>
  string KeyPrefix => "app_runtime_leases/";
}
