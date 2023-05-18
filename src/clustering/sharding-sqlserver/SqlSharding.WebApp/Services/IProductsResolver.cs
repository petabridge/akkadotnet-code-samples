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
}

public sealed class ActorProductsResolver : IProductsResolver
{
    private readonly IActorRef _consumerActor;

    public ActorProductsResolver(IRequiredActor<FetchAllProductsConsumer> consumerActor)
    {
        _consumerActor = consumerActor.ActorRef;
    }

    public async Task<FetchAllProductsResponse> FetchAllProductsAsync(CancellationToken token = default)
    {
        return await _consumerActor.Ask<FetchAllProductsResponse>(FetchAllProducts.Instance, token);
    }
}