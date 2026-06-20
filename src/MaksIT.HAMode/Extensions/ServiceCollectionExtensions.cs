using dotnet_etcd;
using MaksIT.HAMode.Abstractions;
using MaksIT.HAMode.Etcd;
using MaksIT.HAMode.PostgreSql;
using MaksIT.HAMode.Redis;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;

namespace MaksIT.HAMode.Extensions;

/// <summary>
/// DI registration helpers for HAMode abstractions and backend implementations.
/// </summary>
public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers default runtime instance id provider as singleton.
  /// </summary>
  public static IServiceCollection AddHAModeRuntimeInstanceId(this IServiceCollection services) {
    services.AddSingleton<IRuntimeInstanceId, RuntimeInstanceIdProvider>();
    return services;
  }

  /// <summary>
  /// Registers only PostgreSQL-backed runtime lease service.
  /// </summary>
  public static IServiceCollection AddHAModePostgreSqlLease<TConnectionProvider>(this IServiceCollection services)
    where TConnectionProvider : class, IRuntimeLeaseConnectionStringProvider {
    ArgumentNullException.ThrowIfNull(services);

    services.AddSingleton<IRuntimeLeaseConnectionStringProvider, TConnectionProvider>();
    services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceNpgsql>();
    return services;
  }

  /// <summary>
  /// Registers only PostgreSQL-backed runtime lease service using a provided configuration instance.
  /// </summary>
  public static IServiceCollection AddHAModePostgreSqlLease(this IServiceCollection services, IRuntimeLeaseConnectionStringProvider configuration) {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);

    services.AddSingleton(configuration);
    services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceNpgsql>();
    return services;
  }

  /// <summary>
  /// Registers only PostgreSQL-backed runtime lease service using provided configuration and shared data source.
  /// </summary>
  public static IServiceCollection AddHAModePostgreSqlLease(
    this IServiceCollection services,
    IRuntimeLeaseConnectionStringProvider configuration,
    NpgsqlDataSource dataSource
  ) {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);
    ArgumentNullException.ThrowIfNull(dataSource);

    services.AddSingleton(configuration);
    services.AddSingleton(dataSource);
    services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceNpgsql>();
    return services;
  }

  /// <summary>
  /// Registers full PostgreSQL HA mode scenario.
  /// </summary>
  public static IServiceCollection AddHAModePostgreSql<TConnectionProvider>(this IServiceCollection services)
    where TConnectionProvider : class, IRuntimeLeaseConnectionStringProvider {
    return services
      .AddHAModeRuntimeInstanceId()
      .AddHAModePostgreSqlLease<TConnectionProvider>();
  }

  /// <summary>
  /// Registers full PostgreSQL HA mode scenario using a provided configuration instance.
  /// </summary>
  public static IServiceCollection AddHAModePostgreSql(this IServiceCollection services, IRuntimeLeaseConnectionStringProvider configuration) {
    return services
      .AddHAModeRuntimeInstanceId()
      .AddHAModePostgreSqlLease(configuration);
  }

  /// <summary>
  /// Registers full PostgreSQL HA mode scenario using provided configuration and shared data source.
  /// </summary>
  public static IServiceCollection AddHAModePostgreSql(
    this IServiceCollection services,
    IRuntimeLeaseConnectionStringProvider configuration,
    NpgsqlDataSource dataSource
  ) {
    return services
      .AddHAModeRuntimeInstanceId()
      .AddHAModePostgreSqlLease(configuration, dataSource);
  }

  /// <summary>
  /// Registers only Redis-backed runtime lease service.
  /// </summary>
  public static IServiceCollection AddHAModeRedisLease<TConnectionProvider>(this IServiceCollection services)
    where TConnectionProvider : class, IRuntimeLeaseRedisConnectionProvider {
    ArgumentNullException.ThrowIfNull(services);

    services.AddSingleton<IRuntimeLeaseRedisConnectionProvider, TConnectionProvider>();
    services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceRedis>();
    return services;
  }

  /// <summary>
  /// Registers only Redis-backed runtime lease service using a provided configuration instance.
  /// </summary>
  public static IServiceCollection AddHAModeRedisLease(this IServiceCollection services, IRuntimeLeaseRedisConnectionProvider configuration) {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);

    services.AddSingleton(configuration);
    services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceRedis>();
    return services;
  }

  /// <summary>
  /// Registers only Redis-backed runtime lease service using provided configuration and shared multiplexer.
  /// </summary>
  public static IServiceCollection AddHAModeRedisLease(
    this IServiceCollection services,
    IRuntimeLeaseRedisConnectionProvider configuration,
    IConnectionMultiplexer multiplexer
  ) {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);
    ArgumentNullException.ThrowIfNull(multiplexer);

    services.AddSingleton(configuration);
    services.AddSingleton(multiplexer);
    services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceRedis>();
    return services;
  }

  /// <summary>
  /// Registers full Redis HA mode scenario.
  /// </summary>
  public static IServiceCollection AddHAModeRedis<TConnectionProvider>(this IServiceCollection services)
    where TConnectionProvider : class, IRuntimeLeaseRedisConnectionProvider {
    return services
      .AddHAModeRuntimeInstanceId()
      .AddHAModeRedisLease<TConnectionProvider>();
  }

  /// <summary>
  /// Registers full Redis HA mode scenario using a provided configuration instance.
  /// </summary>
  public static IServiceCollection AddHAModeRedis(this IServiceCollection services, IRuntimeLeaseRedisConnectionProvider configuration) {
    return services
      .AddHAModeRuntimeInstanceId()
      .AddHAModeRedisLease(configuration);
  }

  /// <summary>
  /// Registers full Redis HA mode scenario using provided configuration and shared multiplexer.
  /// </summary>
  public static IServiceCollection AddHAModeRedis(
    this IServiceCollection services,
    IRuntimeLeaseRedisConnectionProvider configuration,
    IConnectionMultiplexer multiplexer
  ) {
    return services
      .AddHAModeRuntimeInstanceId()
      .AddHAModeRedisLease(configuration, multiplexer);
  }

  /// <summary>
  /// Registers only etcd-backed runtime lease service.
  /// </summary>
  public static IServiceCollection AddHAModeEtcdLease<TConnectionProvider>(this IServiceCollection services)
    where TConnectionProvider : class, IRuntimeLeaseEtcdConnectionProvider {
    ArgumentNullException.ThrowIfNull(services);

    services.AddSingleton<IRuntimeLeaseEtcdConnectionProvider, TConnectionProvider>();
    services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceEtcd>();
    return services;
  }

  /// <summary>
  /// Registers only etcd-backed runtime lease service using a provided configuration instance.
  /// </summary>
  public static IServiceCollection AddHAModeEtcdLease(this IServiceCollection services, IRuntimeLeaseEtcdConnectionProvider configuration) {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);

    services.AddSingleton(configuration);
    services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceEtcd>();
    return services;
  }

  /// <summary>
  /// Registers only etcd-backed runtime lease service using provided configuration and shared client.
  /// </summary>
  public static IServiceCollection AddHAModeEtcdLease(
    this IServiceCollection services,
    IRuntimeLeaseEtcdConnectionProvider configuration,
    EtcdClient client
  ) {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);
    ArgumentNullException.ThrowIfNull(client);

    services.AddSingleton(configuration);
    services.AddSingleton(client);
    services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceEtcd>();
    return services;
  }

  /// <summary>
  /// Registers full etcd HA mode scenario.
  /// </summary>
  public static IServiceCollection AddHAModeEtcd<TConnectionProvider>(this IServiceCollection services)
    where TConnectionProvider : class, IRuntimeLeaseEtcdConnectionProvider {
    return services
      .AddHAModeRuntimeInstanceId()
      .AddHAModeEtcdLease<TConnectionProvider>();
  }

  /// <summary>
  /// Registers full etcd HA mode scenario using a provided configuration instance.
  /// </summary>
  public static IServiceCollection AddHAModeEtcd(this IServiceCollection services, IRuntimeLeaseEtcdConnectionProvider configuration) {
    return services
      .AddHAModeRuntimeInstanceId()
      .AddHAModeEtcdLease(configuration);
  }

  /// <summary>
  /// Registers full etcd HA mode scenario using provided configuration and shared client.
  /// </summary>
  public static IServiceCollection AddHAModeEtcd(
    this IServiceCollection services,
    IRuntimeLeaseEtcdConnectionProvider configuration,
    EtcdClient client
  ) {
    return services
      .AddHAModeRuntimeInstanceId()
      .AddHAModeEtcdLease(configuration, client);
  }
}
