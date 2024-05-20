using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;

namespace ClusterClientSample.Shared;

public class WorkerActor : ReceiveActor
{
    private readonly ILoggingAdapter _log;
    
    public WorkerActor(IActorRef counter)
    {
        _log = Context.GetLogger();

        ReceiveAsync<Work>(async work =>
        {
            var workResult = await BusinessLogic(work.Id);
            var result = new Result(work.Id, workResult);
            _log.Info("Worker {0} - [{1}]: {2}", Self.Path.Name, result.Id, result.Value);
            Sender.Tell(result);
            counter.Tell(WorkComplete.Instance);
        });
    }

    protected override void PreStart()
    {
        base.PreStart();
        _log.Info("Worker actor started at {0}", Self.Path);
    }
    
    // Simulate a computationally expensive workload
    private static readonly Random Rnd = new ();
    private async Task<int> BusinessLogic(int input)
    {
        await Task.Delay(Rnd.Next(100, 1000));
        return input * 10;
    } 
}