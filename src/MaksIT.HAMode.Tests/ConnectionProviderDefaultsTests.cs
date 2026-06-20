using MaksIT.HAMode.Abstractions;

namespace MaksIT.HAMode.Tests;

public sealed class ConnectionProviderDefaultsTests {
  [Fact]
  public void PostgreSqlProvider_UsesDefaultSchemaAndTable() {
    IRuntimeLeaseConnectionStringProvider provider = new TestPgProvider();
    Assert.Equal("public", provider.Schema);
    Assert.Equal("app_runtime_leases", provider.Table);
  }

  [Fact]
  public void PostgreSqlProvider_ImplementsRootConnectorInterface() {
    IRuntimeLeaseConnectionProvider provider = new TestPgProvider();
    Assert.IsAssignableFrom<IRuntimeLeaseConnectionStringProvider>(provider);
  }

  [Fact]
  public void RedisProvider_UsesDefaultKeyPrefix() {
    IRuntimeLeaseRedisConnectionProvider provider = new TestRedisProvider();
    Assert.Equal("app_runtime_leases:", provider.KeyPrefix);
  }

  [Fact]
  public void RedisProvider_ImplementsRootConnectorInterface() {
    IRuntimeLeaseConnectionProvider provider = new TestRedisProvider();
    Assert.IsAssignableFrom<IRuntimeLeaseRedisConnectionProvider>(provider);
  }

  [Fact]
  public void EtcdProvider_UsesDefaultKeyPrefixAndNullCredentials() {
    IRuntimeLeaseEtcdConnectionProvider provider = new TestEtcdProvider();
    Assert.Equal("app_runtime_leases/", provider.KeyPrefix);
    Assert.Null(provider.Username);
    Assert.Null(provider.Password);
  }

  [Fact]
  public void EtcdProvider_ImplementsRootConnectorInterface() {
    IRuntimeLeaseConnectionProvider provider = new TestEtcdProvider();
    Assert.IsAssignableFrom<IRuntimeLeaseEtcdConnectionProvider>(provider);
  }
}
