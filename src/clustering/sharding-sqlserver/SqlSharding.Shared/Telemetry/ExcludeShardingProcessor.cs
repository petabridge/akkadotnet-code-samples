using System.Diagnostics;
using OpenTelemetry;

namespace SqlSharding.Shared.Telemetry;

/// <summary>
/// Excludes all spans created by the built-in sharding actors
/// </summary>
public class ExcludeShardingProcessor : CompositeProcessor<Activity>
{
    public override void OnEnd(Activity data)
    {
        if (data.Tags.Any(c => c.Key.Equals("akka.actor.type")
                               && c.Value != null
                               && (c.Value.Contains("Akka.Cluster.Sharding"))))
            return; // filter out
        base.OnEnd(data);
    }

    public ExcludeShardingProcessor(IEnumerable<BaseProcessor<Activity>> processors) : base(processors)
    {
    }
}