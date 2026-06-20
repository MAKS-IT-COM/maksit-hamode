using Etcdserverpb;
using Google.Protobuf;
using MaksIT.HAMode.Abstractions;
using MaksIT.Results;
using Microsoft.Extensions.Logging;
using dotnet_etcd;

namespace MaksIT.HAMode.Etcd;

/// <summary>
/// etcd runtime lease implementation using compare-and-swap transactions and TTL leases.
/// </summary>
public sealed class RuntimeLeaseServiceEtcd(
  IRuntimeLeaseEtcdConnectionProvider connectionProvider,
  ILogger<RuntimeLeaseServiceEtcd> logger,
  EtcdClient? sharedClient = null
) : IRuntimeLeaseService {
  private readonly Lazy<EtcdClient> _client = new(() =>
    sharedClient ??
    (!string.IsNullOrWhiteSpace(connectionProvider.Username) && connectionProvider.Password is not null
      ? new EtcdClient(connectionProvider.Endpoints, connectionProvider.Username, connectionProvider.Password)
      : new EtcdClient(connectionProvider.Endpoints)));

  public async Task<Result<bool>> TryAcquireAsync(string leaseName, string holderId, TimeSpan ttl, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(leaseName))

      return Result<bool>.BadRequest(false, "leaseName is required.");
    if (string.IsNullOrWhiteSpace(holderId))
      return Result<bool>.BadRequest(false, "holderId is required.");

    if (ttl <= TimeSpan.Zero)
      return Result<bool>.BadRequest(false, "ttl must be positive.");

    if (sharedClient is null && string.IsNullOrWhiteSpace(connectionProvider.Endpoints))
      return Result<bool>.BadRequest(false, "etcd endpoints are required.");

    if (string.IsNullOrWhiteSpace(connectionProvider.KeyPrefix))
      return Result<bool>.BadRequest(false, "etcd key prefix is required.");

    try {
      var key = BuildKey(leaseName);
      var keyBytes = ByteString.CopyFromUtf8(key);
      var holderBytes = ByteString.CopyFromUtf8(holderId);
      var ttlSeconds = Math.Max(1L, (long)Math.Ceiling(ttl.TotalSeconds));
      var client = _client.Value;

      var leaseGrant = await client.LeaseGrantAsync(new LeaseGrantRequest { TTL = ttlSeconds }).WaitAsync(cancellationToken).ConfigureAwait(false);
      var leaseId = leaseGrant.ID;

      var acquireTxn = new TxnRequest();
      acquireTxn.Compare.Add(new Compare {
        Key = keyBytes,
        Target = Compare.Types.CompareTarget.Create,
        Result = Compare.Types.CompareResult.Equal,
        CreateRevision = 0
      });
      acquireTxn.Success.Add(new RequestOp {
        RequestPut = new PutRequest {
          Key = keyBytes,
          Value = holderBytes,
          Lease = leaseId
        }
      });

      var acquireResponse = await client.TransactionAsync(acquireTxn).WaitAsync(cancellationToken).ConfigureAwait(false);
      if (acquireResponse.Succeeded)
        return Result<bool>.Ok(true);

      var renewTxn = new TxnRequest();
      renewTxn.Compare.Add(new Compare {
        Key = keyBytes,
        Target = Compare.Types.CompareTarget.Value,
        Result = Compare.Types.CompareResult.Equal,
        Value = holderBytes
      });
      renewTxn.Success.Add(new RequestOp {
        RequestPut = new PutRequest {
          Key = keyBytes,
          Value = holderBytes,
          Lease = leaseId
        }
      });

      var renewResponse = await client.TransactionAsync(renewTxn).WaitAsync(cancellationToken).ConfigureAwait(false);
      if (renewResponse.Succeeded)
        return Result<bool>.Ok(true);

      await client.LeaseRevokeAsync(new LeaseRevokeRequest { ID = leaseId }).WaitAsync(cancellationToken).ConfigureAwait(false);
      return Result<bool>.Ok(false);
    }
    catch (Exception ex) {
      logger.LogError(ex, "etcd TryAcquire lease failed for {LeaseName}", leaseName);
      return Result<bool>.InternalServerError(false, ["Lease acquire failed.", ex.Message]);
    }
  }

  public async Task<Result> ReleaseAsync(string leaseName, string holderId, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(leaseName))
      return Result.BadRequest("leaseName is required.");

    if (string.IsNullOrWhiteSpace(holderId))
      return Result.BadRequest("holderId is required.");

    if (sharedClient is null && string.IsNullOrWhiteSpace(connectionProvider.Endpoints))
      return Result.BadRequest("etcd endpoints are required.");
      
    if (string.IsNullOrWhiteSpace(connectionProvider.KeyPrefix))
      return Result.BadRequest("etcd key prefix is required.");

    try {
      var keyBytes = ByteString.CopyFromUtf8(BuildKey(leaseName));
      var holderBytes = ByteString.CopyFromUtf8(holderId);
      var client = _client.Value;

      var releaseTxn = new TxnRequest();
      releaseTxn.Compare.Add(new Compare {
        Key = keyBytes,
        Target = Compare.Types.CompareTarget.Value,
        Result = Compare.Types.CompareResult.Equal,
        Value = holderBytes
      });
      releaseTxn.Success.Add(new RequestOp {
        RequestDeleteRange = new DeleteRangeRequest {
          Key = keyBytes
        }
      });

      _ = await client.TransactionAsync(releaseTxn).WaitAsync(cancellationToken).ConfigureAwait(false);
      return Result.Ok();
    }
    catch (Exception ex) {
      logger.LogWarning(ex, "etcd Release lease failed for {LeaseName} (ignored).", leaseName);
      return Result.InternalServerError(["Lease release failed.", ex.Message]);
    }
  }

  private string BuildKey(string leaseName) => $"{connectionProvider.KeyPrefix}{leaseName}";
}
