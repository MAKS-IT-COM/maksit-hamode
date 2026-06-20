using MaksIT.Results;

namespace MaksIT.HAMode.Abstractions;

/// <summary>Runtime coordination lease API.</summary>
public interface IRuntimeLeaseService {
  Task<Result<bool>> TryAcquireAsync(string leaseName, string holderId, TimeSpan ttl, CancellationToken cancellationToken = default);
  Task<Result> ReleaseAsync(string leaseName, string holderId, CancellationToken cancellationToken = default);
}
