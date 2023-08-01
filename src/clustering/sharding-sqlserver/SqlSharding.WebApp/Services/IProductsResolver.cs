// -----------------------------------------------------------------------
//  <copyright file="IProductsResolver.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Hosting;
using SqlSharding.Shared.Queries;
using SqlSharding.WebApp.Actors;

namespace SqlSharding.WebApp.Services;

public interface IProductsResolver
{
    Task<FetchAllProductsResponse> FetchAllProductsAsync(CancellationToken token = default);
    Task<FetchSoldProductsResponse> FetchSoldProductsAsync(CancellationToken token = default);
    Task<FetchWarningEventsResponse> FetchWarningEventsAsync(CancellationToken token = default);
}

public sealed class ActorProductsResolver : IProductsResolver
{
    private readonly IActorRef _consumerActor;
    private readonly IActorRef _soldConsumerActor;
    private readonly IActorRef _warningEventConsumerActor;

    public ActorProductsResolver(
        IRequiredActor<FetchAllProductsConsumer> consumerActor,
        IRequiredActor<FetchSoldProductsConsumer> soldConsumerActor,
        IRequiredActor<FetchWarningEventsConsumer> warningEventConsumerActor)
    {
        _soldConsumerActor = soldConsumerActor.ActorRef;
        _consumerActor = consumerActor.ActorRef;
        _warningEventConsumerActor = warningEventConsumerActor.ActorRef;
    }

    public async Task<FetchAllProductsResponse> FetchAllProductsAsync(CancellationToken token = default)
    {
        return await _consumerActor.Ask<FetchAllProductsResponse>(FetchAllProducts.Instance, token);
    }

    public async Task<FetchSoldProductsResponse> FetchSoldProductsAsync(CancellationToken token = default)
    {
        return await _soldConsumerActor.Ask<FetchSoldProductsResponse>(FetchSoldProducts.Instance, token);
    }

    public async Task<FetchWarningEventsResponse> FetchWarningEventsAsync(CancellationToken token = default)
    {
        return await _warningEventConsumerActor.Ask<FetchWarningEventsResponse>(FetchWarningEvents.Instance, token);
    }
}