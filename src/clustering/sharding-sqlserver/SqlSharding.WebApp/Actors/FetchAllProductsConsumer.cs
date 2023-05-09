// -----------------------------------------------------------------------
//  <copyright file="FetchAllProductsConsumer.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Delivery;
using Akka.Event;
using Akka.Hosting;
using Akka.Util;
using Akka.Util.Extensions;
using SqlSharding.Shared.Queries;
using SqlSharding.Shared.Sharding;

namespace SqlSharding.WebApp.Actors;

/// <summary>
/// Actor responsible for fetching all products from the database
/// </summary>
public sealed class FetchAllProductsConsumer : UntypedActor, IWithStash, IWithTimers
{
    private readonly IActorRef _productIndexProxy;
    private readonly IActorRef _fetchAllProductsConsumerController;
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly string _producerId; // used to uniquely identify ourselves
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

    private class RequestTimeout
    {
        private RequestTimeout() { }
        public static RequestTimeout Instance { get; } = new();
    }

    public FetchAllProductsConsumer(IRequiredActor<ProductIndexMarker> productIndexProxy)
    {
        _producerId = Guid.NewGuid().ToString();
        _productIndexProxy = productIndexProxy.ActorRef;
        var consumerControllerSettings = ConsumerController.Settings.Create(Context.System);
        var consumerControllerProps =
            ConsumerController.Create<IFetchAllProductsProtocol>(Context, Option<IActorRef>.None,
                consumerControllerSettings);
        _fetchAllProductsConsumerController = Context.ActorOf(consumerControllerProps, "controller");
        _fetchAllProductsConsumerController.Tell(new ConsumerController.Start<IFetchAllProductsProtocol>(Self));
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case FetchAllProducts:
                _productIndexProxy.Tell(new FetchAllProductsImpl(_producerId, _fetchAllProductsConsumerController)); // trigger a query to the index actor
                Context.Become(WaitingForResponse(Sender));
                Timers.StartSingleTimer("request-timeout", RequestTimeout.Instance, DefaultTimeout);
                _log.Debug("Triggering FetchAllProducts query to the index actor for [{0}] with timeout [{1}]", Sender, DefaultTimeout);
                break;
            case RequestTimeout:
                break; // ignore
            case ConsumerController.Delivery<IFetchAllProductsProtocol> { Message: FetchAllProductsResponse resp } response:
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
                case ConsumerController.Delivery<IFetchAllProductsProtocol> { Message: FetchAllProductsResponse resp } response:
                    response.ConfirmTo.Tell(ConsumerController.Confirmed.Instance);
                    requestor.Tell(resp);
                    BecomeActive();
                    return true;
                case FetchAllProducts:
                    Stash.Stash(); // DEFER
                    return true;
                case RequestTimeout _:
                    _log.Error("FetchAllProducts query to the index actor for [{0}] timed out after [{1}]", Sender, DefaultTimeout);
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