using System;
using System.Collections.Generic;
using System.Text;
using Akka.Event;

namespace Akka.Persistence.Failure
{
    public class WorkingPersistentUntypedActor : UntypedPersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private int _currentCount = 0;

        public WorkingPersistentUntypedActor(string persistenceId)
        {
            PersistenceId = persistenceId;
        }

        public override string PersistenceId { get; }
        protected override void OnCommand(object message)
        {
            switch (message)
            {
                case int n:
                    Persist(n, iN =>
                    {
                        _log.Info("Command: Adding [{0}] to current count [{1}] - new count: [{2}]", iN, _currentCount, _currentCount + iN);
                        _currentCount += iN;

                        //if (LastSequenceNr % 20 == 0)
                        //{
                        //    SaveSnapshot(_currentCount);
                        //    _log.Info("Saving snapshot at count of [{0}]", _currentCount);
                        //}
                    });
                    break;
            }
        }

        protected override void OnRecover(object message)
        {
            switch (message)
            {
                case int i:
                    _log.Info("Recovery: Adding [{0}] to current count [{1}] - new count: [{2}]", i, _currentCount, _currentCount + i);
                    _currentCount += i;
                    break;
                case SnapshotOffer o:
                    if (o.Snapshot is int n)
                    {
                        _log.Info("Recovery: Setting initial count to [{1}]", n);
                        _currentCount = n;
                    }

                    break;
            }
        }
    }

    /// <summary>
    /// Persistent actor that is going to try to get its work done using the <see cref="FailingJournal"/>
    /// </summary>
    public class WorkingPersistentActor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private int _currentCount = 0;

        public WorkingPersistentActor(string persistenceId)
        {
            PersistenceId = persistenceId;

            Recover<int>(i =>
            {
                _log.Info("Recovery: Adding [{0}] to current count [{1}] - new count: [{2}]", i, _currentCount, _currentCount + i);
                _currentCount += i;
            });

            Recover<SnapshotOffer>(o =>
            {
                if (o.Snapshot is int i)
                {
                    _log.Info("Recovery: Setting initial count to [{1}]", i);
                    _currentCount = i;
                }
            });

            Command<int>(i =>
            {
                Persist(i, iN =>
                {
                    _log.Info("Command: Adding [{0}] to current count [{1}] - new count: [{2}]", iN, _currentCount, _currentCount + iN);
                    _currentCount += iN;

                    //if (LastSequenceNr % 20 == 0)
                    //{
                    //    SaveSnapshot(_currentCount);
                    //    _log.Info("Saving snapshot at count of [{0}]", _currentCount);
                    //}
                });
            });
        }

        public override string PersistenceId { get; }
    }
}
