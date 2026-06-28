using MaksIT.Results;

namespace MaksIT.HAMode.Abstractions;

internal static class LeaseInputValidation {
  internal static Result<bool>? ValidateAcquireInputs(string leaseName, string holderId, TimeSpan ttl) {
    if (string.IsNullOrWhiteSpace(leaseName))
      return Result<bool>.BadRequest(false, "leaseName is required.");

    if (string.IsNullOrWhiteSpace(holderId))
      return Result<bool>.BadRequest(false, "holderId is required.");

    if (ttl <= TimeSpan.Zero)
      return Result<bool>.BadRequest(false, "ttl must be positive.");

    return null;
  }

  internal static Result? ValidateReleaseInputs(string leaseName, string holderId) {
    if (string.IsNullOrWhiteSpace(leaseName))
      return Result.BadRequest("leaseName is required.");

    if (string.IsNullOrWhiteSpace(holderId))
      return Result.BadRequest("holderId is required.");

    return null;
  }

  internal static Result<bool>? ValidatePostgreSqlProvider(
    IRuntimeLeaseConnectionStringProvider connectionStringProvider,
    bool hasSharedDataSource
  ) {
    if (!hasSharedDataSource && string.IsNullOrWhiteSpace(connectionStringProvider.ConnectionString))
      return Result<bool>.BadRequest(false, "connection string is required.");

    if (string.IsNullOrWhiteSpace(connectionStringProvider.Schema))
      return Result<bool>.BadRequest(false, "schema is required.");

    if (string.IsNullOrWhiteSpace(connectionStringProvider.Table))
      return Result<bool>.BadRequest(false, "table is required.");

    return null;
  }

  internal static Result? ValidatePostgreSqlProviderForRelease(
    IRuntimeLeaseConnectionStringProvider connectionStringProvider,
    bool hasSharedDataSource
  ) {
    if (!hasSharedDataSource && string.IsNullOrWhiteSpace(connectionStringProvider.ConnectionString))
      return Result.BadRequest("connection string is required.");

    if (string.IsNullOrWhiteSpace(connectionStringProvider.Schema))
      return Result.BadRequest("schema is required.");

    if (string.IsNullOrWhiteSpace(connectionStringProvider.Table))
      return Result.BadRequest("table is required.");

    return null;
  }

  internal static Result<bool>? ValidateRedisProvider(
    IRuntimeLeaseRedisConnectionProvider connectionProvider,
    bool hasSharedMultiplexer
  ) {
    if (!hasSharedMultiplexer && string.IsNullOrWhiteSpace(connectionProvider.Configuration))
      return Result<bool>.BadRequest(false, "redis configuration is required.");

    if (string.IsNullOrWhiteSpace(connectionProvider.KeyPrefix))
      return Result<bool>.BadRequest(false, "redis key prefix is required.");

    return null;
  }

  internal static Result? ValidateRedisProviderForRelease(
    IRuntimeLeaseRedisConnectionProvider connectionProvider,
    bool hasSharedMultiplexer
  ) {
    if (!hasSharedMultiplexer && string.IsNullOrWhiteSpace(connectionProvider.Configuration))
      return Result.BadRequest("redis configuration is required.");

    if (string.IsNullOrWhiteSpace(connectionProvider.KeyPrefix))
      return Result.BadRequest("redis key prefix is required.");

    return null;
  }

  internal static Result<bool>? ValidateEtcdProvider(
    IRuntimeLeaseEtcdConnectionProvider connectionProvider,
    bool hasSharedClient
  ) {
    if (!hasSharedClient && string.IsNullOrWhiteSpace(connectionProvider.Endpoints))
      return Result<bool>.BadRequest(false, "etcd endpoints are required.");

    if (string.IsNullOrWhiteSpace(connectionProvider.KeyPrefix))
      return Result<bool>.BadRequest(false, "etcd key prefix is required.");

    return null;
  }

  internal static Result? ValidateEtcdProviderForRelease(
    IRuntimeLeaseEtcdConnectionProvider connectionProvider,
    bool hasSharedClient
  ) {
    if (!hasSharedClient && string.IsNullOrWhiteSpace(connectionProvider.Endpoints))
      return Result.BadRequest("etcd endpoints are required.");

    if (string.IsNullOrWhiteSpace(connectionProvider.KeyPrefix))
      return Result.BadRequest("etcd key prefix is required.");

    return null;
  }
}
