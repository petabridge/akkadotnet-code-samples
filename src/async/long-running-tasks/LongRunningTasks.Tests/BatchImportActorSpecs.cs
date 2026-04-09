using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using LongRunningTasks;
using Xunit;
using Xunit.Abstractions;

namespace LongRunningTasks.Tests;

public class BatchImportActorSpecs : TestKit
{
    public BatchImportActorSpecs(ITestOutputHelper output) : base(nameof(BatchImportActorSpecs), output)
    {
    }

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, resolver) =>
        {
            var importActor = system.ActorOf(
                resolver.Props<BatchImportActor>(), "batch-importer");
            registry.Register<BatchImportActor>(importActor);
        });
    }

    private IActorRef ImportActor => ActorRegistry.Get<BatchImportActor>();

    [Fact]
    public async Task Should_complete_import_successfully()
    {
        // use a small dataset so the test finishes quickly
        ImportActor.Tell(new StartImport(RecordCount: 500, BatchSize: 100));

        // actor should eventually log completion — verify via status polling
        await AwaitConditionAsync(async () =>
        {
            var status = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
            return !status.IsActive && status.RecordsProcessed == 500;
        }, max: TimeSpan.FromSeconds(30));

        var final = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
        Assert.Equal(500, final.RecordsProcessed);
        Assert.Equal(500, final.TotalRecords);
        Assert.False(final.IsActive);
    }

    [Fact]
    public async Task Should_report_status_while_import_is_running()
    {
        // enough records that we can query mid-flight
        ImportActor.Tell(new StartImport(RecordCount: 5000, BatchSize: 100));

        // wait until import is active
        await AwaitConditionAsync(async () =>
        {
            var status = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
            return status.IsActive;
        }, max: TimeSpan.FromSeconds(10));

        // query status while running — the actor should respond (mailbox not blocked)
        var midStatus = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
        Assert.True(midStatus.IsActive);
        Assert.Equal(5000, midStatus.TotalRecords);
        Assert.True(midStatus.Elapsed > TimeSpan.Zero);
    }

    [Fact]
    public async Task Should_cancel_running_import()
    {
        // large enough that we can cancel mid-flight
        ImportActor.Tell(new StartImport(RecordCount: 50_000, BatchSize: 100));

        // wait until import is actively processing
        await AwaitConditionAsync(async () =>
        {
            var status = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
            return status.IsActive && status.RecordsProcessed > 0;
        }, max: TimeSpan.FromSeconds(10));

        // cancel the import
        ImportActor.Tell(new CancelImport());

        // should stop and report not active, with fewer records than total
        await AwaitConditionAsync(async () =>
        {
            var status = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
            return !status.IsActive;
        }, max: TimeSpan.FromSeconds(10));

        var final = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
        Assert.False(final.IsActive);
        Assert.True(final.RecordsProcessed < 50_000,
            $"Expected partial processing after cancel, but got {final.RecordsProcessed}/{final.TotalRecords}");
    }

    [Fact]
    public async Task Should_reject_duplicate_start_while_importing()
    {
        ImportActor.Tell(new StartImport(RecordCount: 50_000, BatchSize: 100));

        // wait until active
        await AwaitConditionAsync(async () =>
        {
            var status = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
            return status.IsActive;
        }, max: TimeSpan.FromSeconds(10));

        // second start should be rejected — actor replies with current status
        var response = await ImportActor.Ask<StatusResponse>(
            new StartImport(RecordCount: 100), TimeSpan.FromSeconds(3));
        Assert.True(response.IsActive);
        Assert.Equal(50_000, response.TotalRecords); // still the original import

        // cleanup
        ImportActor.Tell(new CancelImport());
    }

    [Fact]
    public async Task Should_allow_new_import_after_completion()
    {
        // first import
        ImportActor.Tell(new StartImport(RecordCount: 200, BatchSize: 100));
        await AwaitConditionAsync(async () =>
        {
            var s = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
            return !s.IsActive && s.RecordsProcessed == 200;
        }, max: TimeSpan.FromSeconds(30));

        // second import should work
        ImportActor.Tell(new StartImport(RecordCount: 300, BatchSize: 100));
        await AwaitConditionAsync(async () =>
        {
            var s = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
            return !s.IsActive && s.RecordsProcessed == 300;
        }, max: TimeSpan.FromSeconds(30));

        var final = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
        Assert.Equal(300, final.RecordsProcessed);
        Assert.Equal(300, final.TotalRecords);
    }

    [Fact]
    public async Task Should_allow_new_import_after_cancellation()
    {
        // start and cancel
        ImportActor.Tell(new StartImport(RecordCount: 50_000, BatchSize: 100));
        await AwaitConditionAsync(async () =>
        {
            var s = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
            return s.IsActive && s.RecordsProcessed > 0;
        }, max: TimeSpan.FromSeconds(10));

        ImportActor.Tell(new CancelImport());
        await AwaitConditionAsync(async () =>
        {
            var s = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
            return !s.IsActive;
        }, max: TimeSpan.FromSeconds(10));

        // new import should complete successfully
        ImportActor.Tell(new StartImport(RecordCount: 200, BatchSize: 100));
        await AwaitConditionAsync(async () =>
        {
            var s = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
            return !s.IsActive && s.RecordsProcessed == 200;
        }, max: TimeSpan.FromSeconds(30));

        var final = await ImportActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(3));
        Assert.Equal(200, final.RecordsProcessed);
    }
}
