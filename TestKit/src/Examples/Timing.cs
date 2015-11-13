using System;
using System.Threading;
using Akka.Actor;
using Akka.TestKit.NUnit;
using NUnit.Framework;

namespace TestKitSample.Examples
{
    public class Timing
    {
        public class TimeConstrainedActor : ReceiveActor
        {
            private readonly int _delay;

            public TimeConstrainedActor() : this(0) { }

            public TimeConstrainedActor(int delay)
            {
                _delay = delay; // milliseconds

                Receive<string>(s =>
                {
                    Thread.Sleep(_delay);
                    Sender.Tell("ok");
                });
            }
        }


        public class TimeConstrainedActorSpecs : TestKit
        {
            [TestCase(100, 300)]
            public void Actor_should_respond_within_max_allowable_time(int delay, int cutoff)
            {
                var a = Sys.ActorOf(Props.Create(() => new TimeConstrainedActor(delay)));

                Within(TimeSpan.FromMilliseconds(cutoff), () =>
                {
                    a.Tell("respond to this");
                    ExpectMsg("ok");
                });
            }
        }
    }
}