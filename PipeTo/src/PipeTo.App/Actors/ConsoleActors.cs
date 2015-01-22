using System;
using Akka.Actor;
using Akka.Util.Internal;

namespace PipeTo.App.Actors
{
    /// <summary>
    /// Actor responsilble for writing TO the console
    /// </summary>
    public class ConsoleWriterActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            var str = message as string; //cast the message back into a string
            if (string.IsNullOrEmpty(str)) //invalid message
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("ERROR! Received a non-string object! How did you manage that?!");
                return;
            }

            Console.WriteLine(str);
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Actor responsible for reading FROM the console
    /// </summary>
    public class ConsoleReaderActor : UntypedActor
    {
        /// <summary>
        /// Message used to signal that we just need to read from the console
        /// </summary>
        public class ReadFromConsoleClean { }

        /// <summary>
        /// Message class used to signal that the URL we attempted to parse was
        /// invalid, and thus we need to retry reading from the console.
        /// </summary>
        public class ReadFromConsoleError
        {
            public ReadFromConsoleError(string previousUrl)
            {
                PreviousUrl = previousUrl;
            }

            public string PreviousUrl { get; private set; }
        }

        public const string EscapeString = "exit";

        protected override void OnReceive(object message)
        {
            if (message is ReadFromConsoleError)
                Instructions.PrintRssParseError(message as ReadFromConsoleError);
            Instructions.PrintRssInstructions();
            var read = Console.ReadLine();

            //see if the user typed "exit"
            if (!string.IsNullOrEmpty(read) &&
                read.ToLowerInvariant().Equals(EscapeString))
            {
                Console.WriteLine("Exiting!");
                // shut down the entire actor system via the ActorContext
                // causes MyActorSystem.AwaitTermination(); to stop blocking the current thread
                // and allows the application to exit.
                Context.System.Shutdown();
                return;
            }

            //tell the ConsoleWriterActor what we just read from the console
            #region YOU NEED TO FILL THIS IN
            #endregion

            //tell ourself to "READ FROM CONSOLE AGAIN"
            #region YOU NEED TO FILL THIS IN
            #endregion
        }
    }
}
