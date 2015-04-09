using System;
using Akka.Actor;
using QDFeedParser;

namespace PipeTo.App.Actors
{
    /// <summary>
    /// Actor responsible for figuring out if we're done processing a RSS feed or not
    /// </summary>
    public class FeedParserCoordinator : ReceiveActor
    {
        #region Message types

        /// <summary>
        /// Message class for signaling that the feed has no contents
        /// </summary>
        public class EmptyFeed { }

        /// <summary>
        /// Uses a scheduled task to determine if we've finished downloading all feed content
        /// </summary>
        public class CheckFeedCompleted { }

        /// <summary>
        /// Used to request <see cref="DownloadStats"/> to an external source
        /// </summary>
        public class GetStats { }

        /// <summary>
        /// Used
        /// </summary>
        public class ErrorParsingFeed
        {
            public ErrorParsingFeed(Uri feed)
            {
                Feed = feed;
            }

            public Uri Feed { get; private set; }
        }

        /// <summary>
        /// Message class for signaling that there are assets that still need to be downloaded
        /// </summary>
        public class RemainingDownloadCount
        {
            public RemainingDownloadCount(string feedUrl, int htmlPages, int images)
            {
                Images = images;
                HtmlPages = htmlPages;
                FeedUrl = feedUrl;
            }

            public string FeedUrl { get; private set; }

            public int HtmlPages { get; private set; }

            public int Images { get; private set; }
        }

        /// <summary>
        /// Message class for signaling that assets have been downloaded
        /// </summary>
        public class DownloadComplete
        {
            public DownloadComplete(string feedUrl, int htmlPages, int images)
            {
                Images = images;
                HtmlPages = htmlPages;
                FeedUrl = feedUrl;
            }

            public string FeedUrl { get; private set; }

            public int HtmlPages { get; private set; }

            public int Images { get; private set; }
        }

        /// <summary>
        /// Used to track the download statistics for a specific feed
        /// </summary>
        public class DownloadStats
        {
            public DownloadStats() : this(0, 0, 0, 0) { }

            public DownloadStats(int totalPages, int completedPages, int totalImages, int completedImages)
            {
                CompletedImages = completedImages;
                TotalImages = totalImages;
                CompletedPages = completedPages;
                TotalPages = totalPages;
            }

            public int TotalPages { get; private set; }

            public int CompletedPages { get; private set; }

            public int TotalImages { get; private set; }

            public int CompletedImages { get; private set; }

            public bool IsComplete
            {
                get { return TotalPages != 0 && CompletedPages == TotalPages && CompletedImages == TotalImages; }
            }

            #region Merge / Copy functions

            public DownloadStats Merge(DownloadComplete complete)
            {
                var copy = Copy();
                copy.CompletedImages += complete.Images;
                copy.CompletedPages += complete.HtmlPages;
                return copy;
            }

            public DownloadStats Merge(RemainingDownloadCount remainingDownloadCount)
            {
                var copy = Copy();
                copy.TotalImages += remainingDownloadCount.Images;
                copy.TotalPages += remainingDownloadCount.HtmlPages;
                return copy;
            }

            public DownloadStats Copy()
            {
                return new DownloadStats(TotalPages, CompletedPages, TotalImages, CompletedImages);
            }

            #endregion
        }

        #endregion

        private readonly Uri _feedUri;
        private DownloadStats _downloadStats;
        private readonly string _consoleWriterActorPath;
        private IActorRef _feedParserActor;
        private IActorRef _httpDownloaderActor;

        /// <summary>
        /// This constructor will actually get called by the <see cref="FeedValidatorActor"/> to begin processing messages
        /// </summary>
        public FeedParserCoordinator(Uri feedUri) : this(feedUri, new DownloadStats(), ActorNames.ConsoleWriterActor.Path)
        {
        }

        /// <summary>
        /// This constructor is for unit testing.
        /// </summary>
        public FeedParserCoordinator(Uri feedUri, DownloadStats downloadStats, string consoleWriterActorPath)
        {
            _feedUri = feedUri;
            _downloadStats = downloadStats;
            _consoleWriterActorPath = consoleWriterActorPath;
            Initialize();
        }

        /// <summary>
        /// Used to set up <see cref="Receive"/> methods for <see cref="FeedParserCoordinator"/>
        /// </summary>
        private void Initialize()
        {
            //Give the Sender a copy of our latest DownloadStats
            Receive<GetStats>(stats => Sender.Tell(_downloadStats));

            //Get an update on remaining downloads that need to be processed
            Receive<RemainingDownloadCount>(count =>
            {
                _downloadStats = _downloadStats.Merge(count);
               SendMessage(string.Format("Need to process an additional {0} pages and {1} images for {2}", count.HtmlPages, count.Images, count.FeedUrl));
            });

            //Process an update on completed downloads
            Receive<DownloadComplete>(complete =>
            {
                _downloadStats = _downloadStats.Merge(complete);
                SendMessage(string.Format("Completed processing of {0} pages and {1} images for {2}", complete.HtmlPages, complete.Images, complete.FeedUrl), PipeToSampleStatusCode.Success);

                //Check to see if this is the last outstanding item that needs to be downloaded
                if (_downloadStats.IsComplete)
                {
                    SignalFeedProcessingCompleted();
                }
            });

            //Feed contained no itmes
            Receive<EmptyFeed>(feed => SignalFeedProcessingCompleted());

            Receive<ErrorParsingFeed>(
                feed => SignalFeedProcessingFailure(string.Format("Error parsing feed {0}", _feedUri), _feedUri.ToString()));
        }

     

        protected override void PreStart()
        {
            //create the HttpDownloaderActor first, since the FeedParserActor depends on it
            _httpDownloaderActor = Context.ActorOf(Props.Create(() => new HttpDownloaderActor(_consoleWriterActorPath)));

            _feedParserActor =
                Context.ActorOf(
                    Props.Create(
                        () => new FeedParserActor(new HttpFeedFactory(), _httpDownloaderActor, _consoleWriterActorPath)));

            //send the initial signal to FeedParserActor that it needs to begin consuming the feed
            _feedParserActor.Tell(new FeedParserActor.BeginProcessFeed(_feedUri));
        }

        protected override void PreRestart(Exception reason, object message)
        {
            // Kill any child actors explicitly on shutdown
            Context.Stop(_httpDownloaderActor);
            Context.Stop(_feedParserActor);

            base.PreRestart(reason, message);
        }

        #region Console output methods

        private void SendMessage(string message, PipeToSampleStatusCode pipeToSampleStatus = PipeToSampleStatusCode.Normal)
        {
            //create the message instance
            var consoleMsg = StatusMessageHelper.CreateMessage(message, pipeToSampleStatus);

            //Select the ConsoleWriterActor and send it a message
            Context.ActorSelection(_consoleWriterActorPath).Tell(consoleMsg);
        }

        private void SendDownloadComplete(string message, string feedUri)
        {
            //create the FAILURE message instance
            var consoleMsg = StatusMessageHelper.CreateOperationCompletedSuccessfullyMessage(message, feedUri);

            //Select the ConsoleWriterActor and send it a message
            Context.ActorSelection(_consoleWriterActorPath).Tell(consoleMsg);
        }

        private void SignalFeedProcessingCompleted()
        {
            SendDownloadComplete(string.Format("Completed download of all items for {0}", _feedUri), _feedUri.ToString());

            //Self-terminate - which will also kill off all child actors
            Context.Self.Tell(PoisonPill.Instance);
        }

        private void SignalFeedProcessingFailure(string message, string feedUri)
        {
            //create the FAILURE message instance
            var consoleMsg = StatusMessageHelper.CreateFailureMessage(message, feedUri);

            //Select the ConsoleWriterActor and send it a message
            Context.ActorSelection(_consoleWriterActorPath).Tell(consoleMsg);
        }

        #endregion
    }
}
