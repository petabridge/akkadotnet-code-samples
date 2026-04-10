using System.Diagnostics;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;

namespace LongRunningTasks;

/// <summary>
/// Demonstrates how to run long-running background work inside an Akka.NET actor
/// without blocking the mailbox using the PipeTo + SharedKillSwitch pattern.
/// </summary>
public class BatchImportActor : ReceiveActor
{
    private bool _isImporting;
    private CancellationTokenSource? _importCts;
    private int _recordsProcessed;
    private int _totalRecords;
    private readonly Stopwatch _stopwatch = new();
    private readonly ILoggingAdapter _log = Context.GetLogger();
    
    // materialize stream processors as child actors
    private readonly IMaterializer _materializer = Context.Materializer();

    public BatchImportActor()
    {
        Receive<StartImport>(HandleStartImport);
        Receive<GetStatus>(HandleGetStatus);
        Receive<CancelImport>(HandleCancelImport);
        Receive<ImportCompleted>(HandleImportCompleted);
    }

    private void HandleStartImport(StartImport msg)
    {
        if (_isImporting)
        {
            _log.Warning("Import already in progress — cancel current import first.");
            Sender.Tell(new StatusResponse(_recordsProcessed, _totalRecords,
                _stopwatch.Elapsed, _isImporting));
            return;
        }

        _isImporting = true;
        _recordsProcessed = 0;
        _totalRecords = msg.RecordCount;
        _stopwatch.Restart();

        // Create cancellation handle and kill switch for the stream
        _importCts?.Cancel();
        _importCts?.Dispose();
        _importCts = new CancellationTokenSource();

        var ct = _importCts.Token;
        var batchSize = msg.BatchSize;
        var csvRecords = FakeDataGenerator.Generate(msg.RecordCount);

        // Capture the materializer while we're on the actor's thread

        _log.Info("Starting import of {0} records in batches of {1}...",
            msg.RecordCount, batchSize);

        // KEY PATTERN: _ = RunStreamPipeline(...).PipeTo(Self)
        // The critical fire-and-forget line. HandleStartImport returns immediately
        // after this. The actor's mailbox is free to process GetStatus and CancelImport.
        _ = RunStreamPipeline(csvRecords, batchSize, _materializer, ct).PipeTo(Self);
    }

    private async Task<ImportCompleted> RunStreamPipeline(
        List<CustomerRecord> csvRecords, int batchSize, 
        IMaterializer materializer, CancellationToken ct)
    {
        var recordsProcessed = 0;
        try
        {
            ct.ThrowIfCancellationRequested();

            await Source.From(csvRecords)
                .Via(ct.AsFlow<CustomerRecord>(true))
                .Grouped(batchSize) // Process up to 5 records concurrently
                .SelectAsync(5, async batch =>
                {
                    // Simulate processing work per batch
                    await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
                    return batch.Count();
                })
                .RunWith(Sink.Aggregate<int, int>(0, (total, count) =>
                {
                    recordsProcessed = total + count;
                    // Side-channel: update actor's progress field
                    // Safe because this field is only read (not written) by GetStatus
                    _recordsProcessed = recordsProcessed;
                    return recordsProcessed;
                }), materializer);

            return new ImportCompleted(recordsProcessed, _totalRecords, Success: true);
        }
        // Distinguishes intentional cancellation from unexpected timeouts
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new ImportCompleted(recordsProcessed, _totalRecords,
                Success: false, "Import cancelled");
        }
        catch (Exception ex)
        {
            return new ImportCompleted(recordsProcessed, _totalRecords,
                Success: false, ex.Message);
        }
    }

    private void HandleGetStatus(GetStatus _)
    {
        // This works even while the import stream is running,
        // proving the actor's mailbox is not blocked.
        Sender.Tell(new StatusResponse(
            _recordsProcessed, _totalRecords, _stopwatch.Elapsed, _isImporting));
    }

    private void HandleCancelImport(CancelImport _)
    {
        if (!_isImporting)
        {
            _log.Info("No import in progress to cancel.");
            return;
        }

        _log.Info("Cancelling import...");

        // Terminates the stream too
        _importCts?.Cancel();
        _importCts?.Dispose();
        _importCts = null;
        _isImporting = false;
        _stopwatch.Stop();
    }

    private void HandleImportCompleted(ImportCompleted msg)
    {
        _isImporting = false;
        _stopwatch.Stop();

        if (msg.Success)
        {
            _log.Info("Import completed successfully. Processed {0}/{1} records in {2:F1}s.",
                msg.RecordsProcessed, msg.TotalRecords, _stopwatch.Elapsed.TotalSeconds);
        }
        else
        {
            _log.Warning("Import finished: {0}. Processed {1}/{2} records.",
                msg.Error, msg.RecordsProcessed, msg.TotalRecords);
        }
    }

    // PostStop uses sync Cancel() because PostStop is synchronous.
    // ReceiveAsync handlers can use await CancelAsync() instead.
    protected override void PostStop()
    {
        _importCts?.Cancel();
        _importCts?.Dispose();
        _importCts = null;
        base.PostStop();
    }
}
