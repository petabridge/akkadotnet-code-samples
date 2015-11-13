using System;
using System.Threading;
using Akka.Actor;
using Akka.Event;
using Akka.TestKit.NUnit;
using NUnit.Framework;

namespace TestKitSample.Examples
{
    class Lifecycle
    {
        public class InvalidData { }
        public class InvalidDataException : Exception { }

        /// <summary>
        /// Actor that takes a <see cref="FooRepository"/> and disposes of 
        /// it in <see cref="PostStop"/> to verify that shutdown / lifecycle happens.
        /// </summary>
        public class RepositoryActor : ReceiveActor
        {
            private readonly ILoggingAdapter _log = Context.GetLogger();

            private readonly FooRepository _repo;

            public RepositoryActor(FooRepository repo)
            {
                _repo = repo;

                Receive<InvalidData>(bad =>
                {
                    LogAndThrow();
                });
            }

            private void LogAndThrow()
            {

                _log.Error("Invalid Data!");
                throw new InvalidDataException();
            }

            protected override void PostStop()
            {
                _repo.Dispose();
            }
        }

        public class FooRepository : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }


        [TestFixture]
        public class RepositoryActorSpec : TestKit
        {
            [Test]
            public void RepositoryActor_should_dispose_repo_on_shutdown()
            {
                var repo = new FooRepository();
                var repoActor = Sys.ActorOf(Props.Create(() => new RepositoryActor(repo)));

                // assert repo has not been disposed
                Assert.False(repo.Disposed);

                Sys.Stop(repoActor);

                Thread.Sleep(TimeSpan.FromSeconds(1));

                // assert repo has now been disposed by RepositoryActor's PostStop method
                Assert.True(repo.Disposed);
            }

        }
    }

}
