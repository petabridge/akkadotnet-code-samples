using Akka.Actor;
using Akka.Cluster.Tools.Singleton;

namespace ReliableRabbitMQ.Consumer.Actors;


/// <summary>
/// Utility classes for creating singleton proxies / singletons for AMQP actors
/// </summary>
public static class AmqpConsumerSingletonSettings{
    public const string SingletonActorName = "amqp-consumer";
    public const string SingletonActorRole = "shard-host";
    
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