# Long-Running Tasks in Akka.NET Actors

Demonstrates how to run long-running background work inside an Akka.NET actor without blocking the mailbox, using the **detached Task + PipeTo + SharedKillSwitch** pattern with Akka.Streams.

## Technology

- [Akka.NET](https://getakka.net/)
- [Akka.Hosting](https://github.com/akkadotnet/Akka.Hosting)
- [Akka.Streams](https://getakka.net/articles/streams/introduction.html)
- [Bogus](https://github.com/bchavez/Bogus) (fake data generation)

## Domain

A `BatchImportActor` processes a large CSV-like dataset using an Akka.Streams pipeline. The import runs in the background while the actor remains responsive to status queries and cancellation requests.

## Running This Sample

```shell
cd src/async/long-running-tasks/LongRunningTasks
dotnet run
```

### Available Commands

| Command | Description |
|---------|-------------|
| `start [count]` | Generate fake customer records and start import (default: 10,000) |
| `status` | Query the actor's progress while the import runs |
| `cancel` | Cancel the running import via SharedKillSwitch |
| `quit` | Graceful shutdown |

### Example Session

```
> start
  Sent StartImport(10,000) to actor.
> status
  Active:   True
  Progress: 3,200 / 10,000
  Complete: 32.0%
  Elapsed:  00:03.21
> status
  Active:   True
  Progress: 7,800 / 10,000
  Complete: 78.0%
  Elapsed:  00:07.85
> cancel
  Sent CancelImport to actor.
```

## Key Code Sections

### 1. Fire-and-Forget with PipeTo (`BatchImportActor.cs`)

```csharp
_ = RunStreamPipeline(csvRecords, batchSize, ct, materializer).PipeTo(Self);
```

This is the critical line. `HandleStartImport` returns immediately after calling `PipeTo`. The stream runs on background threads while the actor's mailbox stays free to process `GetStatus` and `CancelImport` messages.

### 2. SharedKillSwitch for Stream Cancellation

`SharedKillSwitch` is Akka.Streams' built-in mechanism to abort a running stream from outside. The actor holds a reference to the kill switch and calls `Shutdown()` when cancellation is requested.

### 3. CancellationTokenSource as Actor State

The actor owns a `CancellationTokenSource` field — the handle for cancelling in-flight async work. `PostStop` uses synchronous `Cancel()` because `PostStop` is synchronous; regular message handlers could use `await CancelAsync()` instead.

### 4. Progress Reporting While Import Runs

The actor responds to `GetStatus` queries during the import, proving the mailbox is not blocked. The stream updates a progress field via a side-channel in the `Sink.Aggregate` callback.

### 5. Intentional vs. Unexpected Cancellation

```csharp
catch (OperationCanceledException) when (ct.IsCancellationRequested)
```

The `when` guard distinguishes intentional user cancellation from unexpected timeouts or other errors.

## Resources

- [Async/Await and PipeTo in Akka.NET](https://getakka.net/articles/actors/receive-actor-api.html#async-and-await)
- [Akka.Streams Documentation](https://getakka.net/articles/streams/introduction.html)
