using Akka.Persistence.Journal;
using CqrsSqlServer.Shared.Events;

namespace CqrsSqlServer.Shared;

public class MessageTagger : IWriteEventAdapter
{
    public const string ChangedEventTag = "Changed";
    public const string SoldEventTag = "Sold";
    public const string WarningEventTag = "Warning";
    
    public string Manifest(object evt)
    {
        return string.Empty;
    }

    public object ToJournal(object evt)
    {
        return evt switch
        {
            ProductInventoryChanged pic => new Tagged(pic, new[] { ChangedEventTag, pic.Reason.ToString() }),
            ProductSold sold => new Tagged(sold, new[] { SoldEventTag }),
            ProductInventoryWarningEvent warning => new Tagged(warning, new [] { WarningEventTag }), 
            _ => evt
        };
    }
}