namespace MaksIT.HAMode.Abstractions;

/// <summary>
/// Canonical lease holder identity for this application instance.
/// Register exactly one implementation as singleton.
/// </summary>
public interface IRuntimeInstanceId {
  /// <summary>Opaque value stored in runtime lease records as holder id.</summary>
  string InstanceId { get; }
}
