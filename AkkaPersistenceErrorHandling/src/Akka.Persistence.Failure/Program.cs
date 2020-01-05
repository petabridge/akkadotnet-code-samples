using System;
using System.Linq;
using Akka.Actor;
using Akka.Pattern;

namespace Akka.Persistence.Failure
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = @"akka.persistence.journal.plugin = ""akka.persistence.journal.failure""
                           akka.persistence.journal.failure.class = """ + typeof(FailingJournal).AssemblyQualifiedName + "\"";

            var actorSystem = ActorSystem.Create("FailingJournal", config);
            var failingActorProps = BackoffSupervisor.PropsWithSupervisorStrategy(
                Props.Create(() => new WorkingPersistentUntypedActor("fuber")), "fuber", TimeSpan.FromMilliseconds(30),
                TimeSpan.FromMilliseconds(2500), 0.1d, SupervisorStrategy.DefaultStrategy);

            var failingActor = actorSystem.ActorOf(failingActorProps);
            
            // 5051 final sum
            foreach (var i in Enumerable.Range(1, 100))
            {
                failingActor.Tell(i);
            }

            Console.ReadLine();
        }
    }
}
