using Akka.Actor;
using Akka.Cluster.Tools.Singleton;

namespace SqlSharding.Shared.Sharding;

/// <summary>
/// Utility classes for creating singleton proxies / singletons for product actors
/// </summary>
public static class ProductActorProps
{
    public const string SingletonActorName = "product-index";
    public const string SingletonActorRole = "host";
    
    public static Props SingletonProps(ActorSystem system, Props underlyingProps)
    {
        return ClusterSingletonManager.Props(underlyingProps,
            ClusterSingletonManagerSettings.Create(system).WithRole(SingletonActorRole).WithSingletonName(SingletonActorName));
    }
    
    public static Props ProductIndexProxyProps(this ActorSystem system)
    {
        return ClusterSingletonProxy.Props($"/user/{SingletonActorName}",
            ClusterSingletonProxySettings.Create(system).WithRole(SingletonActorRole).WithSingletonName(SingletonActorName));
    }
}