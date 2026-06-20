using MaksIT.HAMode.Abstractions;

namespace MaksIT.HAMode.Tests;

internal sealed class TestPgProvider : IRuntimeLeaseConnectionStringProvider {
  public string ConnectionString { get; init; } = "Host=localhost;Port=5432;Database=hamode;Username=hamode;Password=hamode";
  public string Schema { get; init; } = "public";
  public string Table { get; init; } = "app_runtime_leases";
}

internal sealed class TestRedisProvider : IRuntimeLeaseRedisConnectionProvider {
  public string Configuration { get; init; } = "localhost:6379";
  public string KeyPrefix { get; init; } = "app_runtime_leases:";
}

internal sealed class TestEtcdProvider : IRuntimeLeaseEtcdConnectionProvider {
  public string Endpoints { get; init; } = "http://localhost:2379";
  public string? Username { get; init; }
  public string? Password { get; init; }
  public string KeyPrefix { get; init; } = "app_runtime_leases/";
}
