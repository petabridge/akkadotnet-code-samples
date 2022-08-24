using Phobos.Actor.Configuration;

namespace SqlSharding.Shared.Telemetry;

public class OnlyActiveTracesFilter : ITraceFilter
{
    public bool ShouldTraceMessage(object message, bool alreadyInTrace)
    {
        return alreadyInTrace;
    }
}