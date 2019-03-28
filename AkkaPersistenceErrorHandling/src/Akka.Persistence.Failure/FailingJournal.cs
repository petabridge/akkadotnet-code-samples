using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence.Journal;
using Akka.Util;

namespace Akka.Persistence.Failure
{
    /// <summary>
    /// Journal that is subjected to random write and recovery failures
    /// </summary>
    public class FailingJournal : SharedMemoryJournal
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        public override Task ReplayMessagesAsync(IActorContext context, string persistenceId, long fromSequenceNr, long toSequenceNr, long max,
            Action<IPersistentRepresentation> recoveryCallback)
        {
            var fail = ThreadLocalRandom.Current.Next(0, 10) == 0;
            if (fail)
            {
                _log.Warning("Throwing random error upon recovery...");
                throw new ApplicationException("I failed!");
            }

            return base.ReplayMessagesAsync(context, persistenceId, fromSequenceNr, toSequenceNr, max, recoveryCallback);
        }

        protected override async Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<AtomicWrite> messages)
        {
            var fail = ThreadLocalRandom.Current.Next(0, 10) == 0;
            if (fail)
            {
                _log.Warning("Throwing random error upon write");
                // returns a serialization error equivalent
                //return ImmutableList<Exception>.Empty.Add(new ApplicationException("Write failure"));
                // hard write failure 
                throw new ApplicationException("Write failure");
            }

            return await base.WriteMessagesAsync(messages);
        }
    }
}
