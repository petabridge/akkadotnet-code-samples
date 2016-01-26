using System;
using Akka.Actor;
using Akka.Event;
using Akka.TestKit.NUnit;
using NUnit.Framework;

namespace TestKitSample.Examples
{
    public class Watch
    {
        public class User
        {
            public Guid Id { get; private set; }

            public User()
            {
                Id = Guid.NewGuid();
            }
        }

        /// <summary>
        /// Represents a single <see cref="User"/>s session. 
        /// </summary>
        public class UserSessionActor : ReceiveActor
        {
            private readonly ILoggingAdapter _log = Context.GetLogger();

            public UserSessionActor()
            {
                Receive<object>(obj =>
                {
                    _log.Info($"Received: {obj.ToString()}");
                });
            }
        }

        /// <summary>
        /// Tracks all stats for a given <see cref="User"/> session.
        /// Paired for life with the <see cref="UserSessionActor"/> for this user,
        /// and will shut down when that actor shuts down.
        /// </summary>
        public class UserStatsActor : ReceiveActor
        {
            public IActorRef User { get; }

            public UserStatsActor(IActorRef user)
            {
                User = user;
                // paired for life with a UserSessionActor
                Context.Watch(User);

                Receive<Terminated>(terminated =>
                {
                    // if the UserSessionActor we care about dies, so do we
                    if(terminated.ActorRef.Equals(User))
                        Context.Stop(Self);
                });
            }
        }


        [TestFixture]
        public class UserStatsActorSpecs : TestKit
        {
            [Test]
            public void UserStatsActor_should_shut_down_when_UserSessionActor_dies()
            {
                // using a TestProbe as a stand-in / throwaway actor here
                var user = CreateTestProbe("user");
                var stats = Sys.ActorOf(Props.Create(() => new UserStatsActor(user.Ref)));

                // TestActor is now watching the UserStatsActor
                Watch(stats);

                // kill the UserSessionActor
                Sys.Stop(user);

                // verify that the UserStatsActor shut itself down
                ExpectTerminated(stats);
            }
        }
    }
}
