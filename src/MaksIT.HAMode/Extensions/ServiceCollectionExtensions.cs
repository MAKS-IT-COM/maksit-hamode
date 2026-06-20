using MaksIT.HAMode.Abstractions;
using MaksIT.HAMode.Etcd;
using MaksIT.HAMode.PostgreSql;
using MaksIT.HAMode.Redis;
using Microsoft.Extensions.DependencyInjection;

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
    services.AddSingleton<IRuntimeLeaseConnectionStringProvider, TConnectionProvider>();
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
  /// Registers only Redis-backed runtime lease service.
  /// </summary>
  public static IServiceCollection AddHAModeRedisLease<TConnectionProvider>(this IServiceCollection services)
    where TConnectionProvider : class, IRuntimeLeaseRedisConnectionProvider {
    services.AddSingleton<IRuntimeLeaseRedisConnectionProvider, TConnectionProvider>();
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
  /// Registers only etcd-backed runtime lease service.
  /// </summary>
  public static IServiceCollection AddHAModeEtcdLease<TConnectionProvider>(this IServiceCollection services)
    where TConnectionProvider : class, IRuntimeLeaseEtcdConnectionProvider {
    services.AddSingleton<IRuntimeLeaseEtcdConnectionProvider, TConnectionProvider>();
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
}
