using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.TestKit.NUnit;
using Faker;
using NUnit.Framework;

namespace TestKitSample.Examples
{
    public class TestProbe
    {
        public class User
        {
            public Guid Id = new Guid();
        }

        #region Messages
        /// <summary>
        /// Base class for <see cref="User"/>-related messages.
        /// </summary>
        public class UserMessage
        {
            public UserMessage(IActorRef user)
            {
                User = user;
            }

            public IActorRef User { get; }
        }

        public class UserOnline : UserMessage
        {
            public UserOnline(IActorRef user) : base(user) { }
        }

        public class RegisterUser : UserMessage
        {
            public RegisterUser(IActorRef user) : base(user) { }
        }

        public class FriendOnline : UserMessage
        {
            public FriendOnline(IActorRef user) : base(user) { }
        }

        public class Hi { }

        #endregion

        /// <summary>
        /// Represents the session for a given single <see cref="User"/>.
        /// </summary>
        public class UserSessionActor : ReceiveActor
        {
            private readonly ILoggingAdapter _log = Context.GetLogger();
            public User User { get; private set; }
            public IActorRef Presence { get; }

            public UserSessionActor(User user, IActorRef presence)
            {
                User = user;
                Presence = presence;
                Presence.Tell(new RegisterUser(Self));
                Receives();
            }

            private void Receives()
            {
                Receive<UserOnline>(online =>
                {
                    online.User.Tell(new Hi());
                });

                Receive<Hi>(hi =>
                {
                    _log.Info($"{Sender} says hi. I feel so welcome!");
                });
            }
        }


        /// <summary>
        /// Source of truth for which <see cref="User"/>s are userMsg/offline.
        /// Does pub/sub to all users, notifying them of other users being userMsg.
        /// </summary>
        public class FriendsPresenceActor : ReceiveActor
        {
            public readonly HashSet<IActorRef> Subscribers = new HashSet<IActorRef>();
            private ILoggingAdapter _log = Context.GetLogger();

            public FriendsPresenceActor()
            {
                Receives();
            }

            private void Receives()
            {
                Receive<RegisterUser>(user =>
                {
                    Subscribers.Add(user.User);
                    _log.Info($"Added new user, now have {Subscribers.Count} subscribers.");
                });

                Receive<UserOnline>(online =>
                {
                    // DeathWatch the new userMsg
                    Context.Watch(online.User);

                    Publish(online);
                });

                // remove dead actors from our subscriber list
                Receive<Terminated>(dead =>
                {
                    Subscribers.Remove(dead.ActorRef);
                });
            }

            private void Publish(UserOnline msg)
            {
                foreach (var subscriber in Subscribers)
                {
                    // tell all everyone else this userMsg is userMsg
                    if (!subscriber.Equals(msg.User))
                        subscriber.Tell(new UserOnline(msg.User));
                }
            }
        }

        [TestFixture]
        public class TestProbeSpecs : TestKit
        {
            private readonly Fake<User> _fakeUser = new Fake<User>();

            [Test]
            public void UserSessionActor_should_register_itself_with_presence_on_startup()
            {
                // use TestProbe as stand in for FriendsPresenceActor
                var presence = CreateTestProbe("userPresence");
                var userProps = Props.Create(() => new UserSessionActor(_fakeUser.Generate(), presence));
                var user = Sys.ActorOf(userProps);

                presence.ExpectMsgFrom<RegisterUser>(user, message => message.User.Equals(user));
            }

            [Test]
            public void FriendsPresenceActor_should_keep_list_of_registered_users()
            {
                var presence = ActorOfAsTestActorRef<FriendsPresenceActor>("presence");

                // create some users
                var userProps = Props.Create(() => new UserSessionActor(_fakeUser.Generate(), presence));
                var user1 = Sys.ActorOf(userProps);
                var user2 = Sys.ActorOf(userProps);
                var user3 = Sys.ActorOf(userProps);

                AwaitAssert(() => Assert.AreEqual(3, presence.UnderlyingActor.Subscribers.Count));
            }

            [Test]
            public void FriendsPresenceActor_should_alert_other_users_when_friend_comes_online()
            {
                var presence = ActorOfAsTestActorRef<FriendsPresenceActor>("presence");

                // create probes in place of users
                var user1 = CreateTestProbe("user1");
                var user2 = CreateTestProbe("user2");
                var user3 = CreateTestProbe("user3");

                // make those probes send a message I want
                user1.Send(presence, new RegisterUser(user1));
                user2.Send(presence, new RegisterUser(user2));
                user3.Send(presence, new RegisterUser(user3));

                // pre-check we have the right number of subscribers
                Assert.AreEqual(3, presence.UnderlyingActor.Subscribers.Count);

                // trigger by having a probe send msg that I want
                user1.Send(presence, new UserOnline(user1));

                // other user probes should have been informed of new friend presence
                user1.ExpectNoMsg();
                user2.ExpectMsgFrom<UserOnline>(presence, online => online.User.Equals(user1));
                user3.ExpectMsgFrom<UserOnline>(presence, online => online.User.Equals(user1));
            }

            [Test]
            public void UserSessionActors_should_greet_new_users_coming_online()
            {
                var presence = ActorOfAsTestActorRef<FriendsPresenceActor>("presence");

                // create some users
                var userProps = Props.Create(() => new UserSessionActor(_fakeUser.Generate(), presence));
                var user1 = Sys.ActorOf(userProps);
                var user2 = Sys.ActorOf(userProps);
                var user3 = Sys.ActorOf(userProps);

                // pre-check we have the right number of subscribers
                AwaitAssert(() => Assert.AreEqual(3, presence.UnderlyingActor.Subscribers.Count));

                // create probe to be my minion
                var user4 = CreateTestProbe("user4");
                user4.Send(presence, new RegisterUser(user4));
                presence.Tell(new UserOnline(user4.Ref));

                user4.ExpectMsgAllOf<Hi>();
                user4.ExpectMsg<Hi>();
                user4.ExpectMsg<Hi>();
                user4.ExpectMsg<Hi>();
            }

        }

    }
}
