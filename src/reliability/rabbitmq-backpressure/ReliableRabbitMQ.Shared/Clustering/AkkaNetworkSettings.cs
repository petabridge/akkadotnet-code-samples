namespace ReliableRabbitMQ.Shared.Clustering;

public class AkkaNetworkSettings
{
    public string ActorSystemName { get; set; }
    public string ClusterIp { get; set; }
    public int ClusterPort { get; set; }
    public string[] ClusterSeeds { get; set; } = Array.Empty<string>();
}