using System;
using Akka.Actor;
using PipeTo.App.Actors;
using QDFeedParser;

namespace PipeTo.App
{
    class Program
    {
        /// <summary>
        /// The <see cref="ActorSystem"/> reference we're going to use for this demo
        /// </summary>
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            MyActorSystem = ActorSystem.Create("MyFirstActorSystem");

            //Create the actors who are going to validate RSS / ATOM feeds and start the parsing process
            IActorRef feedValidator =
                MyActorSystem.ActorOf(Props.Create(() => new FeedValidatorActor(new HttpFeedFactory(), ActorNames.ConsoleWriterActor.Path)),
                    ActorNames.FeedValidatorActor.Name);

            //Create the actors who are going to read from and write to the console
            IActorRef consoleWriter = MyActorSystem.ActorOf(Props.Create<ConsoleWriterActor>(), ActorNames.ConsoleWriterActor.Name);
            IActorRef consoleReader = MyActorSystem.ActorOf(Props.Create<ConsoleReaderActor>(), ActorNames.ConsoleReaderActor.Name);

            Instructions.PrintWelcome();

            //Tell the console reader that we're ready to begin
            consoleReader.Tell(new ConsoleReaderActor.ReadFromConsoleClean());

            // This blocks the current thread from exiting until MyActorSystem is shut down
            // The ConsoleReaderActor will shut down the ActorSystem once it receives an 
            // "exit" command from the user
            MyActorSystem.AwaitTermination();
        }
    }
}
