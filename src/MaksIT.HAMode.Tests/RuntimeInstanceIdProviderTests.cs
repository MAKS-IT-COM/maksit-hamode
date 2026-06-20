using MaksIT.HAMode.Abstractions;

namespace MaksIT.HAMode.Tests;

[Collection("Environment")]
public sealed class RuntimeInstanceIdProviderTests {
  [Fact]
  public void InstanceId_InKubernetes_UsesPodNameWithoutProcessSuffix() {
    using var kube = new EnvVarScope("KUBERNETES_SERVICE_HOST", "kube-api");
    using var pod = new EnvVarScope("POD_NAME", "hamode-pod-1");
    using var host = new EnvVarScope("HOSTNAME", null);
    using var computer = new EnvVarScope("COMPUTERNAME", null);

    var provider = new RuntimeInstanceIdProvider();

    Assert.Equal("hamode-pod-1", provider.InstanceId);
  }

  [Fact]
  public void InstanceId_OutsideKubernetes_AppendsProcessId() {
    using var kube = new EnvVarScope("KUBERNETES_SERVICE_HOST", null);
    using var pod = new EnvVarScope("POD_NAME", null);
    using var host = new EnvVarScope("HOSTNAME", null);
    using var computer = new EnvVarScope("COMPUTERNAME", "hamode-host");

    var provider = new RuntimeInstanceIdProvider();

    Assert.Equal($"hamode-host-{Environment.ProcessId}", provider.InstanceId);
  }

  private sealed class EnvVarScope : IDisposable {
    private readonly string _name;
    private readonly string? _original;

    public EnvVarScope(string name, string? value) {
      _name = name;
      _original = Environment.GetEnvironmentVariable(name);
      Environment.SetEnvironmentVariable(name, value);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(_name, _original);
  }
}
