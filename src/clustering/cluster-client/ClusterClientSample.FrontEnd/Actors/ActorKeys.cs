namespace ClusterClientSample.FrontEnd.Actors;

// This class will only be used as a Type marker to retrieve the `ClusterClient` actor from the `ActorRegistry`.
// It is not meant to be instantiated in any way.
public sealed class GatewayClusterClientActor
{
    private GatewayClusterClientActor(){ }
}