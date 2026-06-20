namespace MaksIT.HAMode.Abstractions;

/// <summary>
/// Supplies a PostgreSQL connection string for runtime lease persistence.
/// Kept as abstraction so host projects own configuration sources.
/// </summary>
public interface IRuntimeLeaseConnectionStringProvider {
  string ConnectionString { get; }
}
