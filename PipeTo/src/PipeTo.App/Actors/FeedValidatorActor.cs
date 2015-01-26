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
        private readonly string _consoleWriterActorPath;

        public FeedValidatorActor(IFeedFactory feedFactory, string consoleWriterActorPath)
        {
            _feedFactory = feedFactory;
            _consoleWriterActorPath = consoleWriterActorPath;

            //We're using a ReceiveActor, so we can define different receive functions for individual types of messages.
            Receive<string>(s =>
            {
                SendMessage(string.Format("Validating that {0} is a URL...", s));
                if (IsValidUrl(s))
                {
                    SendMessage(string.Format("{0} is a valid URL.", s), PipeToSampleStatusCode.Success);
                    SendMessage(string.Format("Determining if {0} hosts an RSS feed...", s));

                    var feedUri = new Uri(s, UriKind.Absolute);

                    /*
                     * WOAH! What's going on here?
                     * 
                     * We're calling IsValidRssOrAtomFeed(feedUri), an async method that returns Task<bool>.
                     * Rather than waiting on that Task and blocking the Actor or using the AWAIT keyword, which is unsupported,
                     * we're continuing the task and having it transform its results into a IsValidFeed object, which we'll then
                     * PipeTo this actor's inbox!
                     * 
                     * Async programming with Actors essentially means treating the output of asynchronous operations as just new types
                     * of messages. Because of this design, one FeedValidatorActor could validate many RSS / ATOM feeds concurrently without
                     * changing any of this code.
                     */
                    IsValidRssOrAtomFeed(feedUri).ContinueWith(rssValidationResult => new IsValidFeed(feedUri, rssValidationResult.Result), 
                        TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously).PipeTo(Self);
                }
                else
                {
                    //tell the console reader actor that we need to have the user supply a different URL
                    SendValidationFailure(string.Format("{0} is NOT a valid URL.", s), s);
                }
            });

            //Receive function used to process the results of our PipeTo function in the previous Receive<string> block
            Receive<IsValidFeed>(feed =>
            {
                if (!feed.IsValid)
                {
                    //tell the console reader actor that we need to have the user supply a different URL
                    SendValidationFailure(string.Format("{0} is NOT a valid RSS or ATOM feed.", feed.FeedUri), feed.FeedUri.ToString());
                }
                else
                {
                    SendMessage(string.Format("{0} is a valid RSS or ATOM feed.", feed.FeedUri), PipeToSampleStatusCode.Success);
                    
                    //Begin processing the feed!
                    Context.ActorOf(Props.Create(() => new FeedParserCoordinator(feed.FeedUri)));
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

        private void SendMessage(string message, PipeToSampleStatusCode pipeToSampleStatus = PipeToSampleStatusCode.Normal)
        {
            //create the message instance
            var consoleMsg = StatusMessageHelper.CreateMessage(message, pipeToSampleStatus);

            //Select the ConsoleWriterActor and send it a message
            Context.ActorSelection(_consoleWriterActorPath).Tell(consoleMsg);
        }

        private void SendValidationFailure(string message, string feedUri)
        {
            //create the FAILURE message instance
            var consoleMsg = StatusMessageHelper.CreateFailureMessage(message, feedUri);

            //Select the ConsoleWriterActor and send it a message
            Context.ActorSelection(_consoleWriterActorPath).Tell(consoleMsg);
        }

        #endregion
    }
}
