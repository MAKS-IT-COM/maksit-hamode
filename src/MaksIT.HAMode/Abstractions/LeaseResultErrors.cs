using MaksIT.Core.Extensions;
using MaksIT.Results;

namespace MaksIT.HAMode.Abstractions;

internal static class LeaseResultErrors {
  internal static Result<bool> AcquireFailed(Exception exception) =>
    Result<bool>.InternalServerError(false, ["Lease acquire failed.", .. exception.ExtractMessages()]);

  internal static Result ReleaseFailed(Exception exception) =>
    Result.InternalServerError(["Lease release failed.", .. exception.ExtractMessages()]);

  internal static Result<bool> AcquireTableMissing(string qualifiedTableName) =>
    Result<bool>.InternalServerError(false, [
      $"Lease table '{qualifiedTableName}' was not found.",
      "Create the table or set Schema/Table in the PostgreSQL connection provider."
    ]);

  internal static Result ReleaseTableMissing(string qualifiedTableName) =>
    Result.InternalServerError([
      $"Lease table '{qualifiedTableName}' was not found.",
      "Create the table or set Schema/Table in the PostgreSQL connection provider."
    ]);
}
