using System;
using System.Threading;
using Akka.Actor;
using Akka.Event;
using Akka.TestKit.NUnit;
using Akka.TestKit.TestActors;
using NUnit.Framework;

namespace TestKitSample.Examples
{
    #region Messages

    public class CreateUser { }

    public class CheckIfReady { }

    public class GetReady { }

    public class UserResult
    {
        public bool Successful { get; }

        public UserResult() : this(false) { }

        public UserResult(bool successful)
        {
            Successful = successful;
        }
    }
    #endregion


    /// <summary>
    /// This demo actor collaborates with the <see cref="AuthenticationActor"/>
    /// to authenticate <see cref="CreateUser"/> requests before creating those users.
    /// </summary>
    public class IdentityManagerActor : ReceiveActor
    {
        private readonly IActorRef _authenticator;
        private readonly TimeSpan _timeout;

        public IdentityManagerActor(IActorRef authenticationActor) : this(authenticationActor, TimeSpan.FromSeconds(2)) { }

        public IdentityManagerActor(IActorRef authenticationActor, TimeSpan timeout)
        {
            _authenticator = authenticationActor;
            _timeout = timeout;

            Receive<CreateUser>(create =>
            {
                // since we're using the PipeTo pattern, we need to close over state
                // that can change between messages, such as Sender
                // for more, see the PipeTo sample: https://github.com/petabridge/akkadotnet-code-samples/tree/master/PipeTo
                var senderClosure = Sender;

                // this actor needs it create user request to be authenticated
                // within 2 seconds or this operation times out & cancels 
                // the Task returned by Ask<>
                _authenticator.Ask<UserResult>(create, _timeout)
                    .ContinueWith(tr =>
                    {
                        // if the task got messed up / failed, return failure result
                        if (tr.IsCanceled || tr.IsFaulted)
                            return new UserResult(false);

                        // otherwise return whatever the actual result was
                        return tr.Result;
                    }).PipeTo(senderClosure);
            });
        }
    }

    /// <summary>
    /// Authentication counterpart that works with the <see cref="IdentityManagerActor"/>
    /// to authenticate <see cref="CreateUser"/> requests.
    /// </summary>
    public class AuthenticationActor : ReceiveActor
    {
        private readonly TimeSpan _delay;
        public bool Successful { get; }
        public int UsersCreated { get; private set; }
        private bool _ready;
        private ILoggingAdapter _log = Context.GetLogger();

        public AuthenticationActor() : this(TimeSpan.Zero, false) { }

        public AuthenticationActor(TimeSpan delay) : this(delay, false) { }

        public AuthenticationActor(bool successOverride) : this(TimeSpan.Zero, successOverride) { }

        public AuthenticationActor(TimeSpan delay, bool? successOverride = null)
        {
            _delay = delay;
            Successful = successOverride ?? new Random().NextDouble() > 0.5;

            Receive<CreateUser>(create =>
            {
                Thread.Sleep(_delay);
                Sender.Tell(new UserResult(Successful));
                if (Successful)
                    UsersCreated++;

                _log.Info($"Count of users created: {UsersCreated}");
            });

            Receive<CheckIfReady>(check =>
            {
                Sender.Tell(_ready);
            });

            Receive<GetReady>(ready =>
            {
                Thread.Sleep(_delay);
                _ready = true;
            });
        }
    }

    [TestFixture]
    public class IdentityManagerActorSpecs : TestKit
    {
        [Test]
        public void IdentityManagerActor_should_fail_create_user_on_timeout()
        {
            // the BlackHoleActor will NEVER respond to any message sent to it
            // which will force the CreateUser request to time out
            var blackhole = Sys.ActorOf(BlackHoleActor.Props);
            var identity = Sys.ActorOf(Props.Create(() => new IdentityManagerActor(blackhole, TimeSpan.FromSeconds(2))));

            identity.Tell(new CreateUser());
            var result = ExpectMsg<UserResult>().Successful;
            Assert.False(result);
        }

        [Test]
        public void IdentityManagerActor_should_fail_create_user_on_failed_UserResult()
        {
            // make an AuthenticationActor that will always fail to create users
            // by using the constructor argument (normal and PREFERRED approach)
            var authProps = Props.Create(() => new AuthenticationActor(false));
            var auth = Sys.ActorOf(authProps);

            var identityProps = Props.Create(() => new IdentityManagerActor(auth));
            var identity = Sys.ActorOf(identityProps);

            identity.Tell(new CreateUser());
            var result = ExpectMsg<UserResult>().Successful;
            Assert.False(result);
        }

        [TestCase(3, 2)]
        [TestCase(4, 3)]
        public void AuthenticationActor_should_verify_its_ready_within_max_time(int maxSeconds, int authDelaySeconds)
        {
            // create auth actor with 5 second delay built in
            var authProps = Props.Create(() => new AuthenticationActor(TimeSpan.FromSeconds(authDelaySeconds)));
            var auth = Sys.ActorOf(authProps);

            // assert that not ready
            auth.Tell(new CheckIfReady());
            ExpectMsg(false);

            // initialize the AuthenticationActor (with delay)
            auth.Tell(new GetReady());

            // poll every 250ms until AuthenticationActor says it's ready (or we time out)
            AwaitAssert(() =>
            {
                auth.Tell(new CheckIfReady());
                ExpectMsg(true);
            }, TimeSpan.FromSeconds(maxSeconds), TimeSpan.FromMilliseconds(250));
        }

        [Test]
        public void AuthenticationActor_should_keep_internal_count_of_users_created()
        {
            // In this case, we're setting up a ActorOfAsTestActorRef
            // to be able to directly access the internal state of the actor.
            // Generally, THIS IS NOT RECOMMENDED as you want to test the actor
            // from the outside, as it will be used in reality.In real use,
            // actors can't access each others internal state.

            // make an AuthenticationActor that will always successfully create users
            var authProps = Props.Create(() => new AuthenticationActor(true));
            var auth = ActorOfAsTestActorRef<AuthenticationActor>(authProps);

            // ACCESSING INTERNAL ACTOR STATE
            // assert that actor will always return successful ops
            Assert.True(auth.UnderlyingActor.Successful);

            var identityProps = Props.Create(() => new IdentityManagerActor(auth));
            var identity = Sys.ActorOf(identityProps);

            // ACCESSING INTERNAL ACTOR STATE
            // assert that auth user has not created any users
            Assert.AreEqual(auth.UnderlyingActor.UsersCreated, 0);

            // create some users
            var usersToCreate = 4;
            for (int i = 0; i < usersToCreate; i++)
            {
                identity.Tell(new CreateUser());
                var result = ExpectMsg<UserResult>().Successful;
                Assert.True(result);
            }

            // ACCESSING INTERNAL ACTOR STATE
            // assert that count has been kept, and is correct
            Assert.AreEqual(auth.UnderlyingActor.UsersCreated, usersToCreate);
        }

    }
}
