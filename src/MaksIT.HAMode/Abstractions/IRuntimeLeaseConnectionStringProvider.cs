namespace MaksIT.HAMode.Abstractions;

/// <summary>
/// Supplies a PostgreSQL connection string for runtime lease persistence.
/// Kept as abstraction so host projects own configuration sources.
/// </summary>
public interface IRuntimeLeaseConnectionStringProvider : IRuntimeLeaseConnectionProvider {
  /// <summary>PostgreSQL connection string.</summary>
  string ConnectionString { get; }

  /// <summary>Optional schema name for lease table. Defaults to <c>public</c>.</summary>
  string Schema => "public";

  /// <summary>Optional lease table name. Defaults to <c>app_runtime_leases</c>.</summary>
  string Table => "app_runtime_leases";
}
