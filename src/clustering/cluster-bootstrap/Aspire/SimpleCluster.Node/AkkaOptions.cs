using Microsoft.Extensions.Configuration;

namespace SimpleCluster.Node;

public class AkkaOptions
{
    [ConfigurationKeyName("Host_Name")]
    public string HostName { get; set; } = "localhost";
    
    [ConfigurationKeyName("Remote_Port")]
    public int RemotePort { get; set; } = 14884;
    
    [ConfigurationKeyName("Management_Port")]
    public int ManagementPort { get; set; } = 18558;
    
    [ConfigurationKeyName("Discovery_Service_Name")]
    public string DiscoveryServiceName { get; set; } = "akka-discovery";
}