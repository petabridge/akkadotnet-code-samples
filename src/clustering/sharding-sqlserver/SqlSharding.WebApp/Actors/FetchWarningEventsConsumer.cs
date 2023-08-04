using Akka.Actor;
using Akka.Delivery;
using Akka.Event;
using Akka.Hosting;
using Akka.Util;
using SqlSharding.Shared.Queries;
using SqlSharding.Shared.Sharding;

namespace SqlSharding.WebApp.Actors;

public class FetchWarningEventsConsumer : UntypedActor, IWithStash, IWithTimers
{
    private readonly IActorRef _alertIndexProxy;
    private readonly IActorRef _fetchAlertsConsumerController;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly string _producerId; // used to uniquely identify ourselves
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

    private class RequestTimeout
    {
        private RequestTimeout() { }
        public static RequestTimeout Instance { get; } = new();
    }

    public FetchWarningEventsConsumer(IRequiredActor<WarningEventIndexMarker> alertIndexProxy)
    {
        _producerId = Guid.NewGuid().ToString();
        _alertIndexProxy = alertIndexProxy.ActorRef;
        var consumerControllerSettings = ConsumerController.Settings.Create(Context.System);
        var consumerControllerProps =
            ConsumerController.Create<IFetchWarningEventsProtocol>(Context, Option<IActorRef>.None,
                consumerControllerSettings);
        _fetchAlertsConsumerController = Context.ActorOf(consumerControllerProps, "alert-controller");
        _fetchAlertsConsumerController.Tell(new ConsumerController.Start<IFetchWarningEventsProtocol>(Self));
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case FetchWarningEvents:
                _alertIndexProxy.Tell(new FetchWarningEventsImpl(_producerId, _fetchAlertsConsumerController)); // trigger a query to the index actor
                Context.Become(WaitingForResponse(Sender));
                Timers.StartSingleTimer("request-timeout", RequestTimeout.Instance, DefaultTimeout);
                _log.Debug("Triggering FetchAlerts query to the index actor for [{0}] with timeout [{1}]", Sender, DefaultTimeout);
                break;
            case RequestTimeout:
                break; // ignore
            case ConsumerController.Delivery<IFetchWarningEventsProtocol> { Message: FetchWarningEventsResponse resp } response:
                response.ConfirmTo.Tell(ConsumerController.Confirmed.Instance); // old request, but need to confirm
                break;
        }
    }

    private Receive WaitingForResponse(IActorRef requestor)
    {
        return message =>
        {
            switch (message)
            {
                case ConsumerController.Delivery<IFetchWarningEventsProtocol> { Message: FetchWarningEventsResponse resp } response:
                    response.ConfirmTo.Tell(ConsumerController.Confirmed.Instance);
                    requestor.Tell(resp);
                    BecomeActive();
                    return true;
                case FetchWarningEvents:
                    Stash.Stash(); // DEFER
                    return true;
                case RequestTimeout _:
                    _log.Error("FetchAlerts query to the index actor for [{0}] timed out after [{1}]", Sender, DefaultTimeout);
                    BecomeActive();
                    return true;
                default:
                    return false;
            }
        };
    }

    private void BecomeActive()
    {
        Context.Become(OnReceive);
        Stash.UnstashAll();
        Timers.Cancel("request-timeout");
    }

    public IStash Stash { get; set; } = null!;
    public ITimerScheduler Timers { get; set; } = null!;

}