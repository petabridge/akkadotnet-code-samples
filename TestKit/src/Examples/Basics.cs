using Akka.Actor;
using Akka.Event;
using Akka.TestKit.NUnit;
using NUnit.Framework;

namespace TestKitSample.Examples
{
    class UserIdentityActor : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        #region Messages
        public class CreateUser { }
        public class CreateUserWithValidUserInfo : CreateUser { }
        public class CreateUserWithInvalidUserInfo : CreateUser { }
        public class IndexUsers { }

        public class OperationResult
        {
            public bool Successful;
        }
        #endregion

        public UserIdentityActor()
        {
            Receive<CreateUserWithValidUserInfo>(create =>
            {
                // create user here
                Sender.Tell(new OperationResult { Successful = true });
            });

            Receive<CreateUserWithInvalidUserInfo>(create =>
            {
                // fail to create user here
                Sender.Tell(new OperationResult { Successful = false });
            });

            Receive<IndexUsers>(index =>
            {
                _log.Info("indexing users");
                // index the users
            });
        }

    }

    [TestFixture]
    public class UserIdentitySpecs : TestKit
    {
        private IActorRef _identity;

        [SetUp]
        public void Setup()
        {
            _identity = Sys.ActorOf(Props.Create(() => new UserIdentityActor()));
        }

        [Test]
        public void UserIdentityActor_should_confirm_user_creation_success()
        {
            _identity.Tell(new UserIdentityActor.CreateUserWithValidUserInfo());
            var result = ExpectMsg<UserIdentityActor.OperationResult>().Successful;
            Assert.True(result);
        }

        [Test]
        public void UserIdentityActor_should_confirm_user_creation_failure()
        {
            _identity.Tell(new UserIdentityActor.CreateUserWithInvalidUserInfo());
            var result = ExpectMsg<UserIdentityActor.OperationResult>().Successful;
            Assert.False(result);
        }

        [Test]
        public void UserIdentityActor_should_not_respond_to_index_messages()
        {
            _identity.Tell(new UserIdentityActor.IndexUsers());
            ExpectNoMsg();
        }

        [Test]
        public void UserIdentityActor_should_log_user_indexing_operation()
        {
            EventFilter.Info("indexing users").ExpectOne(() =>
            {
                _identity.Tell(new UserIdentityActor.IndexUsers());
            });
        }

    }
}
