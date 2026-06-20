# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
