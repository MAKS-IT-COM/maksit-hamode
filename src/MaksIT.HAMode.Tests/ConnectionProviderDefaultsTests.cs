using MaksIT.HAMode.Abstractions;

namespace MaksIT.HAMode.Tests;

public sealed class ConnectionProviderDefaultsTests {
  [Fact]
  public void RedisProvider_UsesDefaultKeyPrefix() {
    IRuntimeLeaseRedisConnectionProvider provider = new TestRedisProvider();
    Assert.Equal("app_runtime_leases:", provider.KeyPrefix);
  }

  [Fact]
  public void EtcdProvider_UsesDefaultKeyPrefixAndNullCredentials() {
    IRuntimeLeaseEtcdConnectionProvider provider = new TestEtcdProvider();
    Assert.Equal("app_runtime_leases/", provider.KeyPrefix);
    Assert.Null(provider.Username);
    Assert.Null(provider.Password);
  }

  private sealed class TestRedisProvider : IRuntimeLeaseRedisConnectionProvider {
    public string Configuration => "localhost:6379";
  }

  private sealed class TestEtcdProvider : IRuntimeLeaseEtcdConnectionProvider {
    public string Endpoints => "http://localhost:2379";
  }
}
