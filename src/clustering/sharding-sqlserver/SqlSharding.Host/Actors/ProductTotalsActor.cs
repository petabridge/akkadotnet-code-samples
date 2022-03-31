using Akka.Persistence;

namespace SqlSharding.Host.Actors;

public record PurchasingTotals(int RemainingInventory, int SoldInventory, decimal Price)
{
    TotalSold
}

public sealed class ProductTotalsActor : ReceivePersistentActor
{
    /// <summary>
    /// Used to help differentiate what type of entity this is inside Akka.Persistence's database
    /// </summary>
    public const string TotalsEntityNameConstant = "totals";
    
    public ProductTotalsActor(string persistenceId)
    {
        PersistenceId = $"{TotalsEntityNameConstant}-" + persistenceId;
    }

    public override string PersistenceId { get; }
}