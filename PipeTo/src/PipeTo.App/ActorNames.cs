using System;

namespace PipeTo.App
{
    /// <summary>
    /// Helper class that provides basic name and address information for Actors.
    /// 
    /// That way if we need to change the name of an actor, we only need to do it in one place.
    /// </summary>
    public static class ActorNames
    {
        /// <summary>
        /// Responsible for serializing writes to the <see cref="Console"/>
        /// </summary>
		public static readonly ActorData ConsoleWriterActor = new ActorData("consoleWriter", "akka://MyFirstActorSystem/user"); // /user/consoleWriter

        /// <summary>
        /// Responsible for serializing reads from the <see cref="Console"/>
        /// </summary>
		public static readonly ActorData ConsoleReaderActor = new ActorData("consoleReaderActor", "akka://MyFirstActorSystem/user"); // /user/consoleReader

        /// <summary>
        /// Responsible for validating data from <see cref="ConsoleReaderActor"/> and kicking off the feed parsing process
        /// </summary>
		public static readonly ActorData FeedValidatorActor = new ActorData("feedValidator", "akka://MyFirstActorSystem/user"); // /user/feedValidator
    }

    /// <summary>
    /// Meta-data class for working with high-level Actor names and paths
    /// </summary>
    public class ActorData
    {
        public ActorData(string name, string parent)
        {
            Path = parent + "/" + name;
            Name = name;
        }

        public ActorData(string name)
        {
			Path = "akka://MyFirstActorSystem/" + name;
            Name = name;
        }

        public string Name { get; private set; }

        public string Path { get; private set; }
    }
}
