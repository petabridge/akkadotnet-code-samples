using System;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.NUnit;
using NUnit.Framework;

namespace TestKitSample.Examples
{
    public class ScheduleOnceMessage
    {
        public ScheduleOnceMessage(TimeSpan offset, int id)
        {
            Offset = offset;
            Id = id;
        }

        public TimeSpan Offset { get; set; }
        public int Id { get; set; }
    }

    public class ScheduleRepeatedlyMessage
    {
        public TimeSpan InitialOffset { get; set; }
        public TimeSpan RepeatOffset { get; set; }
        public Guid Id { get; set; }

        public ScheduleRepeatedlyMessage(TimeSpan initialOffset, TimeSpan repeatOffset, Guid id)
        {
            InitialOffset = initialOffset;
            RepeatOffset = repeatOffset;
            Id = id;
        }
    }

    public class ScheduledMessages
    {
        /// <summary>
        /// Schedules messages to echo back to sender at designated point(s) in the future.
        /// </summary>
        public class FutureEchoActor : ReceiveActor
        {
            public FutureEchoActor()
            {
                Receives();
            }

            private void Receives()
            {
                Receive<string>(s => Sender.Tell("echo"));

                Receive<ScheduleOnceMessage>(x =>
                {
                    Context.System.Scheduler.ScheduleTellOnce(x.Offset, Sender, x, Self);
                });

                Receive<ScheduleRepeatedlyMessage>(x =>
                {
                    Context.System.Scheduler.ScheduleTellRepeatedly(x.InitialOffset, x.RepeatOffset, Sender, x, Self);
                });
            }
        }


        [TestFixture]
        public class ScheduledMessageActorSpecs : TestKit
        {
            /// <summary>
            /// TestFixture with virtual time <see cref="TestScheduler"/> turned on so we can jump forward in time!
            /// </summary>
            public ScheduledMessageActorSpecs() : base(@"akka.scheduler.implementation = ""Akka.TestKit.TestScheduler, Akka.TestKit""") { }

            /// <summary>
            /// Cast the TestActorSystem scheduler to be a TestScheduler
            /// </summary>
            private TestScheduler Scheduler => (TestScheduler)Sys.Scheduler;

            [Test]
            public void ScheduledMessageActor_should_echo_strings_immediately()
            {
                var actor = Sys.ActorOf(Props.Create(() => new FutureEchoActor()), "echo");
                actor.Tell("hi");
                ExpectMsg("echo");
            }


            [Test]
            public void ScheduledMessageActor_should_schedule_ScheduleOnceMessage_appropriately()
            {
                var actor = Sys.ActorOf(Props.Create(() => new FutureEchoActor()).WithDispatcher(CallingThreadDispatcher.Id));

                var delay1 = TimeSpan.FromHours(1.5);
                actor.Tell(new ScheduleOnceMessage(delay1, 1));
                Scheduler.Advance(delay1);
                var firstId = ExpectMsg<ScheduleOnceMessage>().Id;
                Assert.AreEqual(1, firstId);


                var delay2 = TimeSpan.FromMinutes(8);
                actor.Tell(new ScheduleOnceMessage(delay2, 2));
                Scheduler.AdvanceTo(Scheduler.Now.AddMinutes(8));
                var secondId = ExpectMsg<ScheduleOnceMessage>().Id;
                Assert.AreEqual(2, secondId);
            }

            [Test]
            public void ScheduledMessageActor_should_schedule_ScheduleRepeatedlyMessage_appropriately()
            {
                var actor = Sys.ActorOf(Props.Create(() => new FutureEchoActor()).WithDispatcher(CallingThreadDispatcher.Id));
                var id = new Guid();
                actor.Tell(new ScheduleRepeatedlyMessage(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), id));

                for (int i = 0; i < 300; i++)
                {
                    Scheduler.Advance(TimeSpan.FromSeconds(5));
                    var actual = ExpectMsg<ScheduleRepeatedlyMessage>().Id;
                    Assert.AreEqual(id, actual);
                }
            }
        }


    }
}
