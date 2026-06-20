using MaksIT.HAMode.Abstractions;
using MaksIT.HAMode.Etcd;
using MaksIT.HAMode.PostgreSql;
using MaksIT.HAMode.Redis;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

namespace MaksIT.HAMode.Tests;

public sealed class RuntimeLeaseServiceValidationTests {
  private static readonly TimeSpan PositiveTtl = TimeSpan.FromSeconds(10);

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  public async Task PostgreSql_TryAcquire_InvalidLeaseName_ReturnsBadRequest(string leaseName) {
    var service = new RuntimeLeaseServiceNpgsql(new PgProvider(), NullLogger<RuntimeLeaseServiceNpgsql>.Instance);
    var result = await service.TryAcquireAsync(leaseName, "holder", PositiveTtl, TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Fact]
  public async Task PostgreSql_Release_InvalidHolder_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceNpgsql(new PgProvider(), NullLogger<RuntimeLeaseServiceNpgsql>.Instance);
    var result = await service.ReleaseAsync("lease", "", TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Fact]
  public async Task Redis_TryAcquire_InvalidTtl_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceRedis(new RedisProvider(), NullLogger<RuntimeLeaseServiceRedis>.Instance);
    var result = await service.TryAcquireAsync("lease", "holder", TimeSpan.Zero, TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    await service.DisposeAsync();
  }

  [Fact]
  public async Task Redis_Release_InvalidLeaseName_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceRedis(new RedisProvider(), NullLogger<RuntimeLeaseServiceRedis>.Instance);
    var result = await service.ReleaseAsync("", "holder", TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    await service.DisposeAsync();
  }

  [Fact]
  public async Task Etcd_TryAcquire_InvalidHolder_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceEtcd(new EtcdProvider(), NullLogger<RuntimeLeaseServiceEtcd>.Instance);
    var result = await service.TryAcquireAsync("lease", " ", PositiveTtl, TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Fact]
  public async Task Etcd_Release_InvalidLeaseName_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceEtcd(new EtcdProvider(), NullLogger<RuntimeLeaseServiceEtcd>.Instance);
    var result = await service.ReleaseAsync(" ", "holder", TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  private sealed class PgProvider : IRuntimeLeaseConnectionStringProvider {
    public string ConnectionString => "Host=localhost;Port=5432;Database=hamode;Username=hamode;Password=hamode";
  }

  private sealed class RedisProvider : IRuntimeLeaseRedisConnectionProvider {
    public string Configuration => "localhost:6379";
  }

  private sealed class EtcdProvider : IRuntimeLeaseEtcdConnectionProvider {
    public string Endpoints => "http://localhost:2379";
  }
}
