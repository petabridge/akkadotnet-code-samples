using System.Collections.Immutable;
using Akka.Persistence.Journal;

namespace SqlSharding.Shared;

public class MessageTagger : IWriteEventAdapter
{
    public string Manifest(object evt)
    {
        return null;
    }

    public object ToJournal(object evt)
    {
        return new Tagged(evt, ImmutableList<string>.Empty.Add("shards"));
    }
}