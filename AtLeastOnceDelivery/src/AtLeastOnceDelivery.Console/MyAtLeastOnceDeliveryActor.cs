namespace AtLeastOnceDelivery.Console
{
    using System;
    using Akka.Actor;
    using Akka.Persistence;

    public class MyAtLeastOnceDeliveryActor : AtLeastOnceDeliveryReceiveActor
    {
        // Going to use our name for persistence purposes
        public override string PersistenceId => Context.Self.Path.Name;
        private int _counter = 0;

        private ICancelable _recurringMessageSend;
        private ICancelable _recurringSnapshotCleanup;
        private readonly IActorRef _targetActor;

        private class DoSend { }
        private class CleanSnapshots { }

        const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public MyAtLeastOnceDeliveryActor(IActorRef targetActor)
        {
            _targetActor = targetActor;

            // recover the most recent at least once delivery state
            Recover<SnapshotOffer>(offer => offer.Snapshot is Akka.Persistence.AtLeastOnceDeliverySnapshot, offer =>
            {
                var snapshot = offer.Snapshot as Akka.Persistence.AtLeastOnceDeliverySnapshot;
                SetDeliverySnapshot(snapshot);
            });

            Command<DoSend>(send =>
            {
                Self.Tell(new Write("Message " + Characters[_counter++ % Characters.Length]));
            });

            Command<Write>(write =>
            {
                Deliver(_targetActor.Path, messageId => new ReliableDeliveryEnvelope<Write>(write, messageId));

                // save the full state of the at least once delivery actor
                // so we don't lose any messages upon crash
                SaveSnapshot(GetDeliverySnapshot());
            });

            Command<ReliableDeliveryAck>(ack =>
            {
                ConfirmDelivery(ack.MessageId);
            });

            Command<CleanSnapshots>(clean =>
            {
                // save the current state (grabs confirmations)
                SaveSnapshot(GetDeliverySnapshot());
            });

            Command<SaveSnapshotSuccess>(saved =>
            {
                var seqNo = saved.Metadata.SequenceNr;
                DeleteSnapshots(new SnapshotSelectionCriteria(seqNo, saved.Metadata.Timestamp.AddMilliseconds(-1))); // delete all but the most current snapshot
            });

            Command<SaveSnapshotFailure>(failure =>
            {
                // log or do something else
            });
        }

        protected override void PreStart()
        {
            _recurringMessageSend = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(10), Self, new DoSend(), Self);

            _recurringSnapshotCleanup =
                Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(10), Self, new CleanSnapshots(), ActorRefs.NoSender);

            base.PreStart();
        }

        protected override void PostStop()
        {
            _recurringSnapshotCleanup?.Cancel();
            _recurringMessageSend?.Cancel();

            base.PostStop();
        }
    }
}