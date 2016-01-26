using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.TestKit.NUnit;
using NUnit.Framework;

namespace TestKitSample.Examples
{
    public class ValidInput { }

    public class BadDataShutdown { }

    public class BadDataException : Exception
    {
        public BadDataException(string message) : base(message) { }

        public BadDataException(string message, Exception innerEx) : base(message, innerEx) { }
    }

    public class Supervision
    {
        public static bool IsInputEven(int input)
        {
            return input % 2 == 0;
        }


        public enum MessageType
        {
            Even,
            Odd
        }

        /// <summary>
        /// Creates one <see cref="OddEvenActor"/> child each to handle even/odd inputs. Forwards inputs to appropriate child.
        /// Ignores and logs all other inputs.
        /// Stops children on <see cref="BadDataException"/>s, otherwise restarts them on <see cref="Exception"/>s.
        /// </summary>
        public class OddEvenCoordinatorActor : ReceiveActor
        {

            public static readonly SupervisorStrategy MySupervisorStrategy = new OneForOneStrategy(Decider.From(Directive.Restart,
                new KeyValuePair<Type, Directive>(typeof(BadDataException), Directive.Stop)));

            protected override SupervisorStrategy SupervisorStrategy()
            {
                return MySupervisorStrategy;
            }

            public OddEvenCoordinatorActor()
            {
                var even = Context.ActorOf(Props.Create(() => new OddEvenActor(MessageType.Even)), "even");
                var odd = Context.ActorOf(Props.Create(() => new OddEvenActor(MessageType.Odd)), "odd");

                Receive<int>(i =>
                {
                    if (IsInputEven(i))
                    {
                        even.Forward(i);
                        return;
                    }

                    odd.Forward(i);
                });
            }
        }

        /// <summary>
        /// Actor to receive odd/even inputs from <see cref="OddEvenCoordinatorActor"/>.
        /// </summary>
        public class OddEvenActor : ReceiveActor
        {
            private readonly ILoggingAdapter _log = Context.GetLogger();
            public MessageType Type { get; private set; }

            public OddEvenActor(MessageType type)
            {
                Type = type;
                Receives();
            }

            private void Receives()
            {
                Receive<int>(i =>
                {
                    if (Type == MessageType.Even && !IsInputEven(i) || Type == MessageType.Odd && IsInputEven(i))
                        LogAndThrow($"Got wrong input for {Type} actor.");

                    Sender.Tell(new ValidInput());
                });
            }

            /// <summary>
            /// Logs & throws <see cref="BadDataException"/> when receives an <see cref="int"/> input 
            /// that doesn't match the <see cref="MessageType"/> assigned to this <see cref="OddEvenActor"/>.
            /// </summary>
            /// <param name="reason"></param>
            private void LogAndThrow(string reason)
            {
                throw new BadDataException(reason);
            }

            protected override void PostStop()
            {
                Sender.Tell(new BadDataShutdown());
                _log.Info($"{Self.Path.Name} shutting down.");
            }
        }


        [TestFixture]
        public class SupervisionSpecs : TestKit
        {
            [Test]
            public void OddEvenCoordinatorActor_should_send_odd_input_to_odd_actor()
            {
                // create coordinator, which spins up odd/even child actors
                var coordinator = Sys.ActorOf(Props.Create(() => new OddEvenCoordinatorActor()), "coordinator");

                coordinator.Tell(1);

                // TestActor is sender so it will get reply (coordinator forwards instead of Tells)
                ExpectMsg<ValidInput>();
                Assert.AreEqual("akka://test/user/coordinator/odd", LastSender.Path.ToString());
            }

            [Test]
            public void OddEvenCoordinatorActor_should_send_even_input_to_even_actor()
            {
                // create coordinator, which spins up odd/even child actors
                var coordinator = Sys.ActorOf(Props.Create(() => new OddEvenCoordinatorActor()), "coordinator");

                coordinator.Tell(2);

                // TestActor is sender so it will get reply (coordinator forwards instead of Tells)
                ExpectMsg<ValidInput>();
                Assert.AreEqual("akka://test/user/coordinator/even", LastSender.Path.ToString());
            }

            [Test]
            public void OddEvenActor_should_throw_exception_on_bad_input()
            {
                // create coordinator, which spins up odd/even child actors
                var coordinator = Sys.ActorOf(Props.Create(() => new OddEvenCoordinatorActor()), "coordinator");

                // assert we're set up right
                coordinator.Tell(1);
                coordinator.Tell(2);
                ExpectMsg<ValidInput>();
                ExpectMsg<ValidInput>();

                // test
                var even = ActorSelection("akka://test/user/coordinator/even").ResolveOne(TimeSpan.FromSeconds(5)).Result;
                var odd = ActorSelection("akka://test/user/coordinator/odd").ResolveOne(TimeSpan.FromSeconds(5)).Result;

                // expect exception
                EventFilter.Exception<BadDataException>().Expect(2, () =>
                {
                    even.Tell(1);
                    odd.Tell(2);
                });
            }

            [Test]
            public void OddEvenActor_should_alert_sender_of_shutdown_on_bad_input()
            {
                // create coordinator, which spins up odd/even child actors
                var coordinator = Sys.ActorOf(Props.Create(() => new OddEvenCoordinatorActor()), "coordinator");

                // assert we're set up right
                coordinator.Tell(1);
                coordinator.Tell(2);
                ExpectMsg<ValidInput>();
                ExpectMsg<ValidInput>();

                // test
                var even = ActorSelection("akka://test/user/coordinator/even").ResolveOne(TimeSpan.FromSeconds(5)).Result;
                var odd = ActorSelection("akka://test/user/coordinator/odd").ResolveOne(TimeSpan.FromSeconds(5)).Result;

                // sender should be told that actor shutting down on bad data
                even.Tell(1);
                ExpectMsg<BadDataShutdown>();
                odd.Tell(2);
                ExpectMsg<BadDataShutdown>();

            }

            [Test]
            public void OddEvenCoordinatorActor_should_stop_child_on_bad_data()
            {
                // create coordinator, which spins up odd/even child actors
                var coordinator = Sys.ActorOf(Props.Create(() => new OddEvenCoordinatorActor()), "coordinator");

                // assert we're set up right
                coordinator.Tell(1);
                coordinator.Tell(2);
                ExpectMsg<ValidInput>();
                ExpectMsg<ValidInput>();

                // test
                var even = ActorSelection("akka://test/user/coordinator/even").ResolveOne(TimeSpan.FromSeconds(5)).Result;
                var odd = ActorSelection("akka://test/user/coordinator/odd").ResolveOne(TimeSpan.FromSeconds(5)).Result;

                // even & odd should be killed when parent's SupervisorStrategy kicks in
                Watch(even);
                Watch(odd);

                // we cover BadDataShutdown being sent in another test
                IgnoreMessages(msg => msg is BadDataShutdown);

                // even & odd should be killed when parent's SupervisorStrategy kicks in
                even.Tell(1);
                ExpectTerminated(even);
                odd.Tell(2);
                ExpectTerminated(odd);
            }
        }
    }

}
