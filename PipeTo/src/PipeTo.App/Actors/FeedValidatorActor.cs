using System;
using System.Threading.Tasks;
using Akka.Actor;
using QDFeedParser;

namespace PipeTo.App.Actors
{
    /// <summary>
    /// This actor's job is to validate the following:
    ///  1. That the provided string is, in fact, a URL
    ///  2. That the URL in question is, in fact, an ATOM / RSS feed
    /// </summary>
    public class FeedValidatorActor : ReceiveActor
    {

        #region FeedValidatorActor message types

        /// <summary>
        /// Message class used to pipe the result of whether or not a feed is valid.
        /// </summary>
        public class IsValidFeed
        {
            public IsValidFeed(Uri feedUri, bool isValid)
            {
                IsValid = isValid;
                FeedUri = feedUri;
            }

            public bool IsValid { get; private set; }

            public Uri FeedUri { get; private set; }
        }

        #endregion

        private readonly IFeedFactory _feedFactory;

        public FeedValidatorActor(IFeedFactory feedFactory)
        {
            _feedFactory = feedFactory;

            //We're using a ReceiveActor, so we can define different receive functions for individual types of messages.
            Receive<string>(s =>
            {

                if (IsValidUrl(s))
                {
                    
                }
            });
        }

        #region Feed validation methods

        public bool IsValidUrl(string strToTest)
        {
            return Uri.IsWellFormedUriString(strToTest, UriKind.Absolute);
        }

        public async Task<bool> IsValidRssOrAtomFeed(Uri rssUrl)
        {
            return await _feedFactory.PingFeedAsync(rssUrl);
        }

        #endregion

        #region Console write methods

        public bool SendMessage(string message, bool isSuccess = true)
        {
            var consoleColor = isSuccess ? ConsoleColor.Green : ConsoleColor.DarkRed;
            var consoleMsg = new ConsoleWriterActor.ConsoleWriteMsg(message, consoleColor);
            Context.ActorSelection(ActorNames.ConsoleWriterActor.Path).Tell(consoleMsg);
        }

        #endregion
    }
}
