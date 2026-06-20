using MaksIT.HAMode.Etcd;
using MaksIT.HAMode.PostgreSql;
using MaksIT.HAMode.Redis;
using dotnet_etcd;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using StackExchange.Redis;
using System.Net;

namespace MaksIT.HAMode.Tests;

public sealed class RuntimeLeaseServiceValidationTests {
  private static readonly TimeSpan PositiveTtl = TimeSpan.FromSeconds(10);

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  public async Task PostgreSql_TryAcquire_InvalidLeaseName_ReturnsBadRequest(string leaseName) {
    var service = new RuntimeLeaseServiceNpgsql(new TestPgProvider(), NullLogger<RuntimeLeaseServiceNpgsql>.Instance);
    var result = await service.TryAcquireAsync(leaseName, "holder", PositiveTtl, TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Fact]
  public async Task PostgreSql_Release_InvalidHolder_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceNpgsql(new TestPgProvider(), NullLogger<RuntimeLeaseServiceNpgsql>.Instance);
    var result = await service.ReleaseAsync("lease", "", TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Fact]
  public async Task PostgreSql_TryAcquire_MissingConnectionString_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceNpgsql(
      new TestPgProvider { ConnectionString = "" },
      NullLogger<RuntimeLeaseServiceNpgsql>.Instance);

    var result = await service.TryAcquireAsync("lease", "holder", PositiveTtl, TestContext.Current.CancellationToken);

    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  public async Task PostgreSql_TryAcquire_MissingSchema_ReturnsBadRequest(string schema) {
    var service = new RuntimeLeaseServiceNpgsql(
      new TestPgProvider { Schema = schema },
      NullLogger<RuntimeLeaseServiceNpgsql>.Instance);

    var result = await service.TryAcquireAsync("lease", "holder", PositiveTtl, TestContext.Current.CancellationToken);

    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  public async Task PostgreSql_TryAcquire_MissingTable_ReturnsBadRequest(string table) {
    var service = new RuntimeLeaseServiceNpgsql(
      new TestPgProvider { Table = table },
      NullLogger<RuntimeLeaseServiceNpgsql>.Instance);

    var result = await service.TryAcquireAsync("lease", "holder", PositiveTtl, TestContext.Current.CancellationToken);

    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Fact]
  public async Task PostgreSql_TryAcquire_WithSharedDataSource_AllowsEmptyConnectionString() {
    await using var dataSource = NpgsqlDataSource.Create("Host=127.0.0.1;Port=5432;Database=hamode;Username=hamode;Password=hamode;Timeout=1");
    var service = new RuntimeLeaseServiceNpgsql(
      new TestPgProvider { ConnectionString = "" },
      NullLogger<RuntimeLeaseServiceNpgsql>.Instance,
      dataSource);

    var result = await service.TryAcquireAsync("lease", "holder", PositiveTtl, TestContext.Current.CancellationToken);

    Assert.NotEqual(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Fact]
  public async Task Redis_TryAcquire_InvalidTtl_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceRedis(new TestRedisProvider(), NullLogger<RuntimeLeaseServiceRedis>.Instance);
    var result = await service.TryAcquireAsync("lease", "holder", TimeSpan.Zero, TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    await service.DisposeAsync();
  }

  [Fact]
  public async Task Redis_Release_InvalidLeaseName_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceRedis(new TestRedisProvider(), NullLogger<RuntimeLeaseServiceRedis>.Instance);
    var result = await service.ReleaseAsync("", "holder", TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    await service.DisposeAsync();
  }

  [Fact]
  public async Task Redis_TryAcquire_MissingConfiguration_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceRedis(
      new TestRedisProvider { Configuration = "" },
      NullLogger<RuntimeLeaseServiceRedis>.Instance);

    var result = await service.TryAcquireAsync("lease", "holder", PositiveTtl, TestContext.Current.CancellationToken);

    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    await service.DisposeAsync();
  }

  [Fact]
  public async Task Redis_TryAcquire_MissingKeyPrefix_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceRedis(
      new TestRedisProvider { KeyPrefix = "" },
      NullLogger<RuntimeLeaseServiceRedis>.Instance);

    var result = await service.TryAcquireAsync("lease", "holder", PositiveTtl, TestContext.Current.CancellationToken);

    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    await service.DisposeAsync();
  }

  [Fact]
  public async Task Etcd_TryAcquire_InvalidHolder_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceEtcd(new TestEtcdProvider(), NullLogger<RuntimeLeaseServiceEtcd>.Instance);
    var result = await service.TryAcquireAsync("lease", " ", PositiveTtl, TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Fact]
  public async Task Etcd_Release_InvalidLeaseName_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceEtcd(new TestEtcdProvider(), NullLogger<RuntimeLeaseServiceEtcd>.Instance);
    var result = await service.ReleaseAsync(" ", "holder", TestContext.Current.CancellationToken);
    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Fact]
  public async Task Etcd_TryAcquire_MissingEndpoints_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceEtcd(
      new TestEtcdProvider { Endpoints = "" },
      NullLogger<RuntimeLeaseServiceEtcd>.Instance);

    var result = await service.TryAcquireAsync("lease", "holder", PositiveTtl, TestContext.Current.CancellationToken);

    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Fact]
  public async Task Redis_TryAcquire_WithSharedMultiplexer_AllowsEmptyConfiguration() {
    var multiplexer = ConnectionMultiplexer.Connect("127.0.0.1:63999,abortConnect=false,connectTimeout=1");
    var service = new RuntimeLeaseServiceRedis(
      new TestRedisProvider { Configuration = "" },
      NullLogger<RuntimeLeaseServiceRedis>.Instance,
      multiplexer);

    var result = await service.TryAcquireAsync("lease", "holder", PositiveTtl, TestContext.Current.CancellationToken);

    Assert.NotEqual(HttpStatusCode.BadRequest, result.StatusCode);
    await service.DisposeAsync();
  }

  [Fact]
  public async Task Etcd_TryAcquire_WithSharedClient_AllowsEmptyEndpoints() {
    var client = new EtcdClient("http://127.0.0.1:2379");
    var service = new RuntimeLeaseServiceEtcd(
      new TestEtcdProvider { Endpoints = "" },
      NullLogger<RuntimeLeaseServiceEtcd>.Instance,
      client);

    var result = await service.TryAcquireAsync("lease", "holder", PositiveTtl, TestContext.Current.CancellationToken);

    Assert.NotEqual(HttpStatusCode.BadRequest, result.StatusCode);
  }

  [Fact]
  public async Task Etcd_TryAcquire_MissingKeyPrefix_ReturnsBadRequest() {
    var service = new RuntimeLeaseServiceEtcd(
      new TestEtcdProvider { KeyPrefix = "" },
      NullLogger<RuntimeLeaseServiceEtcd>.Instance);

    var result = await service.TryAcquireAsync("lease", "holder", PositiveTtl, TestContext.Current.CancellationToken);

    Assert.False(result.IsSuccess);
    Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
  }
}
