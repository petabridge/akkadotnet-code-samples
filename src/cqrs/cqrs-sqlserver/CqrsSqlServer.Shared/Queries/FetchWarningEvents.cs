using Akka.Actor;

namespace CqrsSqlServer.Shared.Queries;

public interface IFetchWarningEventsProtocol : ISqlShardingProtocolMember{ }

public class FetchWarningEvents
{
    private FetchWarningEvents(){}
    public static readonly FetchWarningEvents Instance = new();
}

/// <summary>
/// Query to the index actor to retrieve all warning events
/// </summary>
public sealed record FetchWarningEventsImpl(string ProducerId, IActorRef ConsumerController) : ISqlShardingProtocolMember;

public sealed class FetchWarningEventsResponse: IFetchWarningEventsProtocol
{
    public FetchWarningEventsResponse(IReadOnlyList<WarningEventData> warnings)
    {
        Warnings = warnings;
    }

    public IReadOnlyList<WarningEventData> Warnings { get; }
}