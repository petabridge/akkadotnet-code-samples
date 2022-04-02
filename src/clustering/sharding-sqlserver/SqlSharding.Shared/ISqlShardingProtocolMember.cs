namespace SqlSharding.Shared;

/// <summary>
/// Marker interface for all events / apps / state in the domain.
///
/// Used by Akka.NET to select the correct serializer.
/// </summary>
public interface ISqlShardingProtocolMember
{
    
}