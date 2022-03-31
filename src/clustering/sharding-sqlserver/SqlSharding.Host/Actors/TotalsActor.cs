using Akka.Persistence;

namespace SqlSharding.Host.Actors;

public sealed class TotalsActor : ReceivePersistentActor
{
    /// <summary>
    /// Used to help differentiate what type of entity this is inside Akka.Persistence's database
    /// </summary>
    public const string TotalsEntityNameConstant = "totals";
    
    public TotalsActor(string persistenceId)
    {
        PersistenceId = $"{TotalsEntityNameConstant}-" + persistenceId;
    }

    public override string PersistenceId { get; }
}