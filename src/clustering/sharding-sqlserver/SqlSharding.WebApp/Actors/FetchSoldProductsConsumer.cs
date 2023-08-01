// -----------------------------------------------------------------------
//  <copyright file="FetchSoldProductsConsumer.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Delivery;
using Akka.Event;
using Akka.Hosting;
using Akka.Util;
using SqlSharding.Shared.Queries;
using SqlSharding.Shared.Sharding;

namespace SqlSharding.WebApp.Actors;

public class FetchSoldProductsConsumer : UntypedActor, IWithStash, IWithTimers
{
    private readonly IActorRef _productIndexProxy;
    private readonly IActorRef _fetchSoldProductsConsumerController;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly string _producerId; // used to uniquely identify ourselves
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

    private class RequestTimeout
    {
        private RequestTimeout() { }
        public static RequestTimeout Instance { get; } = new();
    }

    public FetchSoldProductsConsumer(IRequiredActor<SoldProductIndexMarker> soldProductIndexProxy)
    {
        _producerId = Guid.NewGuid().ToString();
        _productIndexProxy = soldProductIndexProxy.ActorRef;
        var consumerControllerSettings = ConsumerController.Settings.Create(Context.System);
        var consumerControllerProps =
            ConsumerController.Create<IFetchSoldProductsProtocol>(Context, Option<IActorRef>.None,
                consumerControllerSettings);
        _fetchSoldProductsConsumerController = Context.ActorOf(consumerControllerProps, "sold-controller");
        _fetchSoldProductsConsumerController.Tell(new ConsumerController.Start<IFetchSoldProductsProtocol>(Self));
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case FetchSoldProducts:
                _productIndexProxy.Tell(new FetchSoldProductsImpl(_producerId, _fetchSoldProductsConsumerController)); // trigger a query to the index actor
                Context.Become(WaitingForResponse(Sender));
                Timers.StartSingleTimer("request-timeout", RequestTimeout.Instance, DefaultTimeout);
                _log.Debug("Triggering FetchSoldProducts query to the index actor for [{0}] with timeout [{1}]", Sender, DefaultTimeout);
                break;
            case RequestTimeout:
                break; // ignore
            case ConsumerController.Delivery<IFetchSoldProductsProtocol> { Message: FetchSoldProductsResponse resp } response:
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
                case ConsumerController.Delivery<IFetchSoldProductsProtocol> { Message: FetchSoldProductsResponse resp } response:
                    response.ConfirmTo.Tell(ConsumerController.Confirmed.Instance);
                    requestor.Tell(resp);
                    BecomeActive();
                    return true;
                case FetchSoldProducts:
                    Stash.Stash(); // DEFER
                    return true;
                case RequestTimeout _:
                    _log.Error("FetchSoldProducts query to the index actor for [{0}] timed out after [{1}]", Sender, DefaultTimeout);
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