using dotnet_etcd;
using MaksIT.HAMode.Abstractions;
using MaksIT.HAMode.Etcd;
using MaksIT.HAMode.Extensions;
using MaksIT.HAMode.PostgreSql;
using MaksIT.HAMode.Redis;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;

namespace MaksIT.HAMode.Tests;

public sealed class ServiceCollectionExtensionsTests {
  [Fact]
  public void AddHAModeRuntimeInstanceId_RegistersProvider() {
    var services = new ServiceCollection();
    services.AddHAModeRuntimeInstanceId();

    var provider = services.BuildServiceProvider();
    var instanceId = provider.GetRequiredService<IRuntimeInstanceId>();

    Assert.IsType<RuntimeInstanceIdProvider>(instanceId);
  }

  [Fact]
  public void AddHAModePostgreSql_WithConfigurationInstance_RegistersLeaseService() {
    var configuration = new TestPgProvider();
    var services = new ServiceCollection()
      .AddLogging()
      .AddHAModePostgreSql(configuration);

    var provider = services.BuildServiceProvider();

    Assert.Same(configuration, provider.GetRequiredService<IRuntimeLeaseConnectionStringProvider>());
    Assert.IsType<RuntimeLeaseServiceNpgsql>(provider.GetRequiredService<IRuntimeLeaseService>());
    Assert.IsType<RuntimeInstanceIdProvider>(provider.GetRequiredService<IRuntimeInstanceId>());
  }

  [Fact]
  public void AddHAModePostgreSql_WithSharedDataSource_RegistersLeaseService() {
    var configuration = new TestPgProvider();
    var dataSource = NpgsqlDataSource.Create("Host=127.0.0.1;Port=5432;Database=hamode;Username=hamode;Password=hamode");
    var services = new ServiceCollection()
      .AddLogging()
      .AddHAModePostgreSql(configuration, dataSource);

    var provider = services.BuildServiceProvider();

    Assert.Same(configuration, provider.GetRequiredService<IRuntimeLeaseConnectionStringProvider>());
    Assert.Same(dataSource, provider.GetRequiredService<NpgsqlDataSource>());
    Assert.IsType<RuntimeLeaseServiceNpgsql>(provider.GetRequiredService<IRuntimeLeaseService>());
  }

  [Fact]
  public void AddHAModeRedis_WithConfigurationInstance_RegistersLeaseService() {
    var configuration = new TestRedisProvider();
    var services = new ServiceCollection()
      .AddLogging()
      .AddHAModeRedis(configuration);

    var provider = services.BuildServiceProvider();

    Assert.Same(configuration, provider.GetRequiredService<IRuntimeLeaseRedisConnectionProvider>());
    Assert.IsType<RuntimeLeaseServiceRedis>(provider.GetRequiredService<IRuntimeLeaseService>());
  }

  [Fact]
  public void AddHAModeRedis_WithSharedMultiplexer_RegistersLeaseService() {
    var configuration = new TestRedisProvider();
    var multiplexer = ConnectionMultiplexer.Connect("127.0.0.1:63999,abortConnect=false,connectTimeout=1");
    var services = new ServiceCollection()
      .AddLogging()
      .AddHAModeRedis(configuration, multiplexer);

    var provider = services.BuildServiceProvider();

    Assert.Same(configuration, provider.GetRequiredService<IRuntimeLeaseRedisConnectionProvider>());
    Assert.Same(multiplexer, provider.GetRequiredService<IConnectionMultiplexer>());
    Assert.IsType<RuntimeLeaseServiceRedis>(provider.GetRequiredService<IRuntimeLeaseService>());
  }

  [Fact]
  public void AddHAModeEtcd_WithConfigurationInstance_RegistersLeaseService() {
    var configuration = new TestEtcdProvider();
    var services = new ServiceCollection()
      .AddLogging()
      .AddHAModeEtcd(configuration);

    var provider = services.BuildServiceProvider();

    Assert.Same(configuration, provider.GetRequiredService<IRuntimeLeaseEtcdConnectionProvider>());
    Assert.IsType<RuntimeLeaseServiceEtcd>(provider.GetRequiredService<IRuntimeLeaseService>());
  }

  [Fact]
  public void AddHAModeEtcd_WithSharedClient_RegistersLeaseService() {
    var configuration = new TestEtcdProvider();
    var client = new EtcdClient("http://127.0.0.1:2379");
    var services = new ServiceCollection()
      .AddLogging()
      .AddHAModeEtcd(configuration, client);

    var provider = services.BuildServiceProvider();

    Assert.Same(configuration, provider.GetRequiredService<IRuntimeLeaseEtcdConnectionProvider>());
    Assert.Same(client, provider.GetRequiredService<EtcdClient>());
    Assert.IsType<RuntimeLeaseServiceEtcd>(provider.GetRequiredService<IRuntimeLeaseService>());
  }
}
