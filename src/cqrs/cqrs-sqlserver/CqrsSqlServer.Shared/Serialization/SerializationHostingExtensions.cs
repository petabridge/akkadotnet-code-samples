using Akka.Hosting;

namespace CqrsSqlServer.Shared.Serialization;

public static class SerializationHostingExtensions
{
    /// <summary>
    /// Configures the custom serialization for the SqlSharding App.
    /// </summary>
    public static AkkaConfigurationBuilder AddAppSerialization(this AkkaConfigurationBuilder builder)
    {
        return builder.WithCustomSerializer("sql-sharding", new[] { typeof(ISqlShardingProtocolMember) },
            system => new MessageSerializer(system));
    }
}