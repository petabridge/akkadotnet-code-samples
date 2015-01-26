using System;
using Akka;
using Akka.Actor;

namespace PipeTo.App.Actors
{
    /// <summary>
    /// Actor responsilble for writing TO the console
    /// </summary>
    public class ConsoleWriterActor : UntypedActor
    {
        #region ConsoleWriterActor message types

        /*
         * You don't have to define message types for actors inside that actor's class definition.
         * Any C# type is a valid message type.
         * 
         * However, for messages that are received only by one specific type of actor, this convention
         * of using nested class types is helpful as it makes the intended recipient of the message
         * obvious.
         */

        /// <summary>
        /// Message class used to send colored console messages
        /// </summary>
        public class ConsoleWriteMsg
        {
            public ConsoleWriteMsg(string message)
                : this(message, StatusMessageHelper.MapConsoleColor(PipeToSampleStatusCode.Normal))
            {
            }

            public ConsoleWriteMsg(string message, ConsoleColor color)
            {
                Color = color;
                Message = message;
            }

            public ConsoleColor Color { get; private set; }
            public string Message { get; private set; }
        }

        /// <summary>
        /// Used for sending failure messages, which mean we need to ask the console for additional input
        /// </summary>
        public class ConsoleWriteFailureMessage : ConsoleWriteMsg {
            public ConsoleWriteFailureMessage(string message, string url) : this(message, url, StatusMessageHelper.MapConsoleColor(PipeToSampleStatusCode.Normal))
            {
            }

            public ConsoleWriteFailureMessage(string message, string url, ConsoleColor color) : base(message, color)
            {
                Url = url;
            }

            public string Url { get; private set; }
        }

        /// <summary>
        /// Used for sending "task complete!" messages, which mean we need to ask the console for additional input
        /// </summary>
        public class ConsoleWriteTaskCompleteMessage : ConsoleWriteMsg
        {
            public ConsoleWriteTaskCompleteMessage(string message, string url)
                : this(message, url, StatusMessageHelper.MapConsoleColor(PipeToSampleStatusCode.Normal))
            {
            }

            public ConsoleWriteTaskCompleteMessage(string message, string url, ConsoleColor color) : base(message, color)
            {
                Url = url;
            }

            public string Url { get; private set; }
        }

        #endregion

        protected override void OnReceive(object message)
        {
            /*
             * This actor can receieve either a ConsoleWriteMsg object or a string,
             * it will conditionally check for either upon each received message and
             * mark any other types of messages as 'Unhandled' in the event that an
             * unsupported type is sent to this actor.
             */
            Console.ResetColor();
            var consoleWriteMsg = message as ConsoleWriteMsg;
            if (consoleWriteMsg != null)
            {
                Console.ForegroundColor = consoleWriteMsg.Color;
                Console.WriteLine("{0}: {1}", Sender, consoleWriteMsg.Message);
                Console.ResetColor();

                //use Akka.NET's pattern match to filter for ConsoleWriteMsg sub-types
                consoleWriteMsg.Match()
                    .With<ConsoleWriteTaskCompleteMessage>(completeMessage => //successful operation - tell the ConsoleReaderActor that we're ready for more work.
                        Context.ActorSelection(ActorNames.ConsoleReaderActor.Path).Tell(new ConsoleReaderActor.ReadFromConsoleClean()))
                    .With<ConsoleWriteFailureMessage>(failureMessage => Context.ActorSelection(ActorNames.ConsoleReaderActor.Path)
                        .Tell(new ConsoleReaderActor.ReadFromConsoleError(failureMessage.Url.ToString()))) //failed operation
                    .Default(o =>
                    { 
                        //do nothing
                    });
                return;
            }

            var str = message as string; //cast the message back into a string
            if (string.IsNullOrEmpty(str)) //invalid message
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("{0}: ERROR! Received a non-string object! How did you manage that?!", Sender);
                Unhandled(message);
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
        #region ConsoleReaderActor Message classes
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

        #endregion

        public const string EscapeString = "exit";

        protected override void OnReceive(object message)
        {
            //If the message is neither of our supported message types
            if (!(message is ReadFromConsoleError || message is ReadFromConsoleClean))
            {
                //let's the system know this message was unhandled. Gets published to log.
                Unhandled(message);
                return;
            }

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

            //Tell the FeedValidatorActor that we're ready to party
            Context.ActorSelection(ActorNames.FeedValidatorActor.Path).Tell(read);
        }
    }
}
