namespace LongRunningTasks;

public record StartImport(int RecordCount = 10_000, int BatchSize = 100);

public record GetStatus;

public record StatusResponse(int RecordsProcessed, int TotalRecords, TimeSpan Elapsed, bool IsActive);

public record CancelImport;

public record ImportCompleted(int RecordsProcessed, int TotalRecords, bool Success, string? Error = null);
