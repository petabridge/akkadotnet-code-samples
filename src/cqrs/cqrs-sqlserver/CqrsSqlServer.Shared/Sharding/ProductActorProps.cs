using Akka.Actor;
using Akka.Cluster.Tools.Singleton;

namespace CqrsSqlServer.Shared.Sharding;

/// <summary>
/// Key type for DI
/// </summary>
public sealed class ProductIndexMarker
{
    public static readonly ProductIndexMarker Instance = new();
    private ProductIndexMarker(){}
}

/// <summary>
/// Key type for DI
/// </summary>
public sealed class SoldProductIndexMarker
{
    public static readonly SoldProductIndexMarker Instance = new();
    private SoldProductIndexMarker(){}
}


/// <summary>
/// Key type for DI
/// </summary>
public sealed class WarningEventIndexMarker
{
    public static readonly WarningEventIndexMarker Instance = new();
    private WarningEventIndexMarker(){}
}

/// <summary>
/// Key type for DI
/// </summary>
public sealed class ProductMarker
{
    public static readonly ProductMarker Instance = new ProductMarker();
    private ProductMarker(){}
}

/// <summary>
/// Utility classes for creating singleton proxies / singletons for product actors
/// </summary>
public static class ProductActorProps
{
    public const string SingletonActorName = "product-index";
    public const string SingletonActorRole = "host";
    
    public static Props ProductSingletonProps(this ActorSystem system, Props underlyingProps)
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