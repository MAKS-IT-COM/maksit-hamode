namespace MaksIT.HAMode.Abstractions;

/// <summary>
/// Default runtime instance id provider for HA replicas.
/// In Kubernetes it stays stable for pod lifetime; elsewhere it appends process id.
/// </summary>
public class RuntimeInstanceIdProvider : IRuntimeInstanceId {
  public string InstanceId { get; } = Build();

  private static string Build() {
    var logicalHost =
      Environment.GetEnvironmentVariable("POD_NAME")
      ?? Environment.GetEnvironmentVariable("HOSTNAME")
      ?? Environment.GetEnvironmentVariable("COMPUTERNAME")
      ?? Environment.MachineName;

    if (RunsInKubernetes())
      return logicalHost;

    return $"{logicalHost}-{Environment.ProcessId}";
  }

  private static bool RunsInKubernetes() =>
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST"));
}
