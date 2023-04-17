using System.Collections.Immutable;
using Akka.Persistence.Journal;
using SqlSharding.Shared.Events;

namespace SqlSharding.Shared;

public class MessageTagger : IWriteEventAdapter
{
    public string Manifest(object evt)
    {
        return string.Empty;
    }

    public object ToJournal(object evt)
    {
        return evt is not TaggedEvent te ? evt : new Tagged(te.Event, te.Tags);
    }
}