using Akka.Persistence.Journal;
using CqrsSqlServer.Shared.Events;

namespace CqrsSqlServer.Shared;

public sealed class MessageTagger : IWriteEventAdapter
{
    public const string ChangedEventTag = "Changed";
    public const string SoldEventTag = "Sold";
    public const string WarningEventTag = "Warning";
    public const string ProductEventTag = "ProductEvent";
    
    public string Manifest(object evt)
    {
        return string.Empty;
    }

    public object ToJournal(object evt)
    {
        return evt switch
        {
            ProductInventoryChanged pic => new Tagged(pic, new[] { ProductEventTag, ChangedEventTag, pic.Reason.ToString() }),
            ProductSold sold => new Tagged(sold, new[] {ProductEventTag,  SoldEventTag }),
            ProductInventoryWarningEvent warning => new Tagged(warning, new [] { ProductEventTag, WarningEventTag }), 
            _ => evt
        };
    }
}