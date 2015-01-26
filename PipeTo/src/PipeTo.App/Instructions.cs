using System;
using PipeTo.App.Actors;

namespace PipeTo.App
{
    /// <summary>
    /// Prints colored, friendly instructions on the console.
    /// </summary>
    public static class Instructions
    {
        public static void PrintWelcome()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Hello there!");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("In this sample we're going to ask you to give us an RSS feed address.");
            Console.Write("We're going to use ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("ONE");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" actor to download every web page and image");
            Console.WriteLine();
            Console.WriteLine("in the feed.");
            Console.WriteLine();
            Console.Write("We're doing this to demonstrate how you can use ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" PipeTo() ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.WriteLine("to execute asynchronous operations inside an Actor.");
        }

        public static void PrintRssInstructions()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("So let's start by typing in an RSS feed URL into the console.");
            Console.WriteLine("Don't have one on-hand? Try http://petabridge.com/blog/feed.xml");
            Console.WriteLine();
            Console.ResetColor();
            PrintExitInstructions();
        }

        public static void PrintExitInstructions()
        {
            Console.WriteLine();
            Console.Write("Or you can type ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(" 'exit' ");
            Console.ResetColor();
            Console.Write(" to exit.");
            Console.WriteLine();
        }

        public static void PrintRssParseError(ConsoleReaderActor.ReadFromConsoleError consoleError)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("ERROR! {0} was not a valid URL or RSS could not be parsed.", consoleError.PreviousUrl);
            Console.WriteLine("Please try a different URL!");
            Console.WriteLine();
            Console.ResetColor();
        }
    }
}
