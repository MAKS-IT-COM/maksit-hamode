# MaksIT.HAMode

Reusable high-availability runtime coordination library for MaksIT services.

![Line Coverage](assets/badges/coverage-lines.svg) ![Branch Coverage](assets/badges/coverage-branches.svg) ![Method Coverage](assets/badges/coverage-methods.svg)

## Packages

- `MaksIT.HAMode` (single NuGet package)
  - `MaksIT.HAMode.Abstractions` namespace:
    - `IRuntimeInstanceId`
    - `IRuntimeLeaseService`
    - `IRuntimeLeaseConnectionStringProvider`
    - `IRuntimeLeaseRedisConnectionProvider`
    - `IRuntimeLeaseEtcdConnectionProvider`
    - `RuntimeInstanceIdProvider`
  - `MaksIT.HAMode.PostgreSql.RuntimeLeaseServiceNpgsql`
  - `MaksIT.HAMode.Redis.RuntimeLeaseServiceRedis`
  - `MaksIT.HAMode.Etcd.RuntimeLeaseServiceEtcd`

## Target framework

- `net10.0`

## Design goals

- Keep runtime coordination reusable across products.
- Keep application configuration ownership in host projects.
- Keep lease persistence implementation swappable and SOLID-compliant.

## PostgreSQL lease table contract

`RuntimeLeaseServiceNpgsql` expects:

- table: `public.app_runtime_leases`
- columns:
  - `lease_name` (text, PK)
  - `holder_id` (text)
  - `version` (bigint)
  - `acquired_at_utc` (timestamptz)
  - `expires_at_utc` (timestamptz)

## Usage examples

### Install package

```xml
<ItemGroup>
  <PackageReference Include="MaksIT.HAMode" Version="0.1.0" />
</ItemGroup>
```

### Shared runtime instance id

```csharp
using MaksIT.HAMode.Abstractions;

builder.Services.AddSingleton<IRuntimeInstanceId, RuntimeInstanceIdProvider>();
```

### PostgreSQL backend

```csharp
using MaksIT.HAMode.Abstractions;
using MaksIT.HAMode.PostgreSql;

public sealed class MyPgLeaseConnectionProvider(IConfiguration cfg) : IRuntimeLeaseConnectionStringProvider {
  public string ConnectionString => cfg["Configuration:Engine:ConnectionString"]!;
}

builder.Services.AddSingleton<IRuntimeLeaseConnectionStringProvider, MyPgLeaseConnectionProvider>();
builder.Services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceNpgsql>();
```

### Redis backend

```csharp
using MaksIT.HAMode.Abstractions;
using MaksIT.HAMode.Redis;

public sealed class MyRedisLeaseConnectionProvider(IConfiguration cfg) : IRuntimeLeaseRedisConnectionProvider {
  public string Configuration => cfg["Configuration:Redis:ConnectionString"]!;
  public string KeyPrefix => "my-app/runtime-leases:";
}

builder.Services.AddSingleton<IRuntimeLeaseRedisConnectionProvider, MyRedisLeaseConnectionProvider>();
builder.Services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceRedis>();
```

### etcd backend

```csharp
using MaksIT.HAMode.Abstractions;
using MaksIT.HAMode.Etcd;

public sealed class MyEtcdLeaseConnectionProvider(IConfiguration cfg) : IRuntimeLeaseEtcdConnectionProvider {
  public string Endpoints => cfg["Configuration:Etcd:Endpoints"]!; // ex: http://etcd:2379
  public string? Username => cfg["Configuration:Etcd:Username"];
  public string? Password => cfg["Configuration:Etcd:Password"];
  public string KeyPrefix => "my-app/runtime-leases/";
}

builder.Services.AddSingleton<IRuntimeLeaseEtcdConnectionProvider, MyEtcdLeaseConnectionProvider>();
builder.Services.AddSingleton<IRuntimeLeaseService, RuntimeLeaseServiceEtcd>();
```

### Runtime acquire/release flow

```csharp
using MaksIT.HAMode.Abstractions;

public sealed class BootstrapHostedService(
  IRuntimeLeaseService leaseService,
  IRuntimeInstanceId runtimeInstance,
  ILogger<BootstrapHostedService> logger
) : IHostedService {
  public async Task StartAsync(CancellationToken cancellationToken) {
    var holder = runtimeInstance.InstanceId;
    var acquired = await leaseService.TryAcquireAsync(
      leaseName: "my-app-bootstrap",
      holderId: holder,
      ttl: TimeSpan.FromSeconds(30),
      cancellationToken: cancellationToken);

    if (!acquired.IsSuccess || !acquired.Value) {
      logger.LogInformation("Another replica owns bootstrap lease.");
      return;
    }

    try {
      // Run single-replica bootstrap logic here.
    }
    finally {
      await leaseService.ReleaseAsync("my-app-bootstrap", holder, CancellationToken.None);
    }
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

## Integration in MaksIT.Vault

### 1) Add package reference

In `MaksIT.Vault.Engine.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="MaksIT.HAMode" Version="0.1.0" />
</ItemGroup>
```

Switching backend uses a different service registration (no package change needed).

### 2) Keep Vault interfaces stable with adapters

```csharp
// Vault Engine contract remains unchanged for consumers.
using SharedRuntimeLeaseService = MaksIT.HAMode.Abstractions.IRuntimeLeaseService;
namespace MaksIT.Vault.Engine.Infrastructure;
public interface IRuntimeLeaseService : SharedRuntimeLeaseService;
```

```csharp
// Vault host contract remains unchanged for controllers/hosted services.
namespace MaksIT.Vault.Engine.RuntimeCoordination;
public interface IRuntimeInstanceId : MaksIT.HAMode.Abstractions.IRuntimeInstanceId;
```

### 3) Adapter for connection/config ownership

```csharp
using MaksIT.HAMode.Abstractions;
using SharedRuntimeLeaseServiceNpgsql = MaksIT.HAMode.PostgreSql.RuntimeLeaseServiceNpgsql;

public sealed class RuntimeLeaseServiceNpgsql(
  IVaultEngineConfiguration config,
  ILogger<SharedRuntimeLeaseServiceNpgsql> logger
) : IRuntimeLeaseService {
  private sealed class VaultLeaseConnection(IVaultEngineConfiguration cfg) : IRuntimeLeaseConnectionStringProvider {
    public string ConnectionString => cfg.ConnectionString;
  }

  private readonly SharedRuntimeLeaseServiceNpgsql _inner = new(new VaultLeaseConnection(config), logger);

  public Task<MaksIT.Results.Result<bool>> TryAcquireAsync(string leaseName, string holderId, TimeSpan ttl, CancellationToken ct = default) =>
    _inner.TryAcquireAsync(leaseName, holderId, ttl, ct);

  public Task<MaksIT.Results.Result> ReleaseAsync(string leaseName, string holderId, CancellationToken ct = default) =>
    _inner.ReleaseAsync(leaseName, holderId, ct);
}
```

### 4) DI registration in `Program.cs`

```csharp
builder.Services.AddSingleton<MaksIT.Vault.Engine.RuntimeCoordination.IRuntimeInstanceId, RuntimeInstanceIdProvider>();
builder.Services.AddSingleton<MaksIT.Vault.Engine.Infrastructure.IRuntimeLeaseService, RuntimeLeaseServiceNpgsql>();
```

## Integration in MaksIT.CertsUI

The same pattern applies to `MaksIT.CertsUI.Engine`.

### 1) Add package reference

In `MaksIT.CertsUI.Engine.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="MaksIT.HAMode" Version="0.1.0" />
</ItemGroup>
```

### 2) Keep CertsUI interfaces stable with adapters

```csharp
using SharedRuntimeLeaseService = MaksIT.HAMode.Abstractions.IRuntimeLeaseService;
namespace MaksIT.CertsUI.Engine.Infrastructure;
public interface IRuntimeLeaseService : SharedRuntimeLeaseService;
```

```csharp
namespace MaksIT.CertsUI.Engine.RuntimeCoordination;
public interface IRuntimeInstanceId : MaksIT.HAMode.Abstractions.IRuntimeInstanceId;
```

### 3) Adapter for CertsUI configuration

```csharp
using MaksIT.HAMode.Abstractions;
using SharedRuntimeLeaseServiceNpgsql = MaksIT.HAMode.PostgreSql.RuntimeLeaseServiceNpgsql;

public sealed class RuntimeLeaseServiceNpgsql(
  ICertsEngineConfiguration config,
  ILogger<SharedRuntimeLeaseServiceNpgsql> logger
) : IRuntimeLeaseService {
  private sealed class CertsLeaseConnection(ICertsEngineConfiguration cfg) : IRuntimeLeaseConnectionStringProvider {
    public string ConnectionString => cfg.ConnectionString;
  }

  private readonly SharedRuntimeLeaseServiceNpgsql _inner = new(new CertsLeaseConnection(config), logger);

  public Task<MaksIT.Results.Result<bool>> TryAcquireAsync(string leaseName, string holderId, TimeSpan ttl, CancellationToken ct = default) =>
    _inner.TryAcquireAsync(leaseName, holderId, ttl, ct);

  public Task<MaksIT.Results.Result> ReleaseAsync(string leaseName, string holderId, CancellationToken ct = default) =>
    _inner.ReleaseAsync(leaseName, holderId, ct);
}
```

### 4) DI registration in `Program.cs`

```csharp
builder.Services.AddSingleton<MaksIT.CertsUI.Engine.RuntimeCoordination.IRuntimeInstanceId, RuntimeInstanceIdProvider>();
builder.Services.AddSingleton<MaksIT.CertsUI.Engine.Infrastructure.IRuntimeLeaseService, RuntimeLeaseServiceNpgsql>();
```

## Backend switching strategy

Use the same app-level interface (`IRuntimeLeaseService`) and swap only adapter implementation:

- PostgreSQL: `MaksIT.HAMode.PostgreSql.RuntimeLeaseServiceNpgsql`
- Redis: `MaksIT.HAMode.Redis.RuntimeLeaseServiceRedis`
- etcd: `MaksIT.HAMode.Etcd.RuntimeLeaseServiceEtcd`

This keeps hosted services and domain workflows unchanged in both `maksit-vault` and `maksit-certs-ui`.

## Local pack

```powershell
dotnet pack .\src\MaksIT.HAMode.slnx -c Release
```

The command emits a single `MaksIT.HAMode` `.nupkg` and `.snupkg`.
