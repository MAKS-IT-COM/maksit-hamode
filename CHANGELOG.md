# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.4] - 2026-06-20

### Changed
- Replaced preview dependencies with latest stable releases: `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Logging.Abstractions` (`10.0.8`), and `StackExchange.Redis` (`2.8.58`).

## [1.0.3] - 2026-06-20

### Changed
- Consolidated source into a single `MaksIT.HAMode` project and assembly; removed internal multi-project packaging shell and manual DLL bundling.
- Namespaces are unchanged (`MaksIT.HAMode.Abstractions`, `MaksIT.HAMode.PostgreSql`, etc.) so consumers can upgrade without code changes.

## [1.0.2] - 2026-06-20

### Changed
- PostgreSQL lease service now uses configurable `Schema` and `Table` from `IRuntimeLeaseConnectionStringProvider` instead of hardcoded `public.app_runtime_leases`.
- Added explicit missing-table error handling for PostgreSQL lease acquire/release operations.
- Added provider-configuration validation for Redis and etcd services (required connection settings and key prefix).
- Added DI overloads to reuse host-managed clients (`NpgsqlDataSource`, `IConnectionMultiplexer`, `EtcdClient`) for lease services.

## [1.0.1] - 2026-06-20

### Added
- DI registration extensions in `MaksIT.HAMode.Extensions.ServiceCollectionExtensions` for runtime instance id and backend-specific lease service wiring (`PostgreSql`, `Redis`, `Etcd`).
- DI extension overloads that accept concrete configuration instances implementing HAMode configuration interfaces.
- Root connector configuration interface `IRuntimeLeaseConnectionProvider`, with connector-specific interfaces inheriting it.

### Changed
- Updated package/release setup to publish `MaksIT.HAMode` as the primary distributable library for version `1.0.1`.
- Updated several dependency versions across HAMode projects.
- Updated README backend examples to use host-defined configuration interfaces and concrete classes, instead of direct `IConfiguration["..."]` access.

## [1.0.0] - 2026-06-20

### Added
- Initial NuGet package structure for `MaksIT.HAMode.Abstractions` and `MaksIT.HAMode.PostgreSql`.
- Shared runtime coordination contracts:
  - `IRuntimeInstanceId`
  - `IRuntimeLeaseService`
  - `IRuntimeLeaseConnectionStringProvider`
- Default runtime instance id provider (`RuntimeInstanceIdProvider`) suitable for Kubernetes and local processes.
- PostgreSQL lease service (`RuntimeLeaseServiceNpgsql`) implementing optimistic lease acquire and safe holder-based release.
- Package metadata, symbols, embedded docs, and release assets for NuGet publishing.
- Additional runtime lease backend packages:
  - `MaksIT.HAMode.Redis` with atomic Lua acquire/release semantics and TTL renewal for the same holder.
  - `MaksIT.HAMode.Etcd` with transaction-based compare-and-swap acquire/release and lease-backed TTL.

### Changed
- Packaging model changed to a single distributable NuGet package: `MaksIT.HAMode`.
- Internal projects (`Abstractions`, `PostgreSql`, `Redis`, `Etcd`) remain as source modules but are no longer individually packed.
