using System;
using Akka.Actor;
using HtmlAgilityPack;
using QDFeedParser;

namespace PipeTo.App.Actors
{
    /// <summary>
    /// Uses QDFeedParser's <see cref="IFeedFactory"/> to parse RSS / Atom Feeds. https://github.com/Aaronontheweb/qdfeed
    /// 
    /// And also uses HTML Agility Pack to parse img tags from any downloaded HTML. http://htmlagilitypack.codeplex.com/
    /// </summary>
    public class FeedParserActor : ReceiveActor
    {
        #region Message types

        /// <summary>
        /// Message sent by <see cref="FeedParserCoordinator"/> that begins the parsing process
        /// </summary>
        public class BeginProcessFeed
        {
            public BeginProcessFeed(Uri feedUri)
            {
                FeedUri = feedUri;
            }

            public Uri FeedUri { get; private set; }
        }

        /// <summary>
        /// Feed Content that needs to be parsed for image tags
        /// </summary>
        public class ParseFeedItem
        {
            public ParseFeedItem(string feedUri, BaseFeedItem feedItem)
            {
                FeedItem = feedItem;
                FeedUri = feedUri;
            }

            public string FeedUri { get; private set; }

            public BaseFeedItem FeedItem { get; private set; }
        }

        #endregion

        private readonly IFeedFactory _feedFactory;
        private IActorRef _downloadActor;
        private readonly string _consoleWriterActorPath;

        public FeedParserActor(IFeedFactory feedFactory, IActorRef downloadActor) 
            : this(feedFactory, downloadActor, ActorNames.ConsoleWriterActor.Path)
        {
        }

        public FeedParserActor(IFeedFactory feedFactory, IActorRef downloadActor, string consoleWriterActorPath)
        {
            _feedFactory = feedFactory;
            _downloadActor = downloadActor;
            _consoleWriterActorPath = consoleWriterActorPath;

            //Set our Receive functions
            Initialize();
        }

        public void Initialize()
        {
            //time to kick off the feed parsing process, and send the results to ourselves
            Receive<BeginProcessFeed>(feed =>
            {
                SendMessage(string.Format("Downloading {0} for RSS/ATOM processing...", feed.FeedUri));
                _feedFactory.CreateFeedAsync(feed.FeedUri).PipeTo(Self);
            });

            //this is the type of message we receive when the BeginProcessFeed's PipeTo operation finishes
            Receive<IFeed>(feed =>
            {
                SendMessage("Feed download successful.", PipeToSampleStatusCode.Success);
                SendMessage(string.Format("Have to download and parse {0} pages", feed.Items.Count));

                //We have to download at least one HTML page per feed URL.
                Context.Parent.Tell(new FeedParserCoordinator.RemainingDownloadCount(feed.FeedUri, feed.Items.Count, 0));

                //for each item in the feed, we need to process it.
                foreach (var item in feed.Items)
                {
                    //Check to see if there's any HTML content...
                    if (!string.IsNullOrEmpty(item.Content))
                    {
                        //We're going to self process each of these items
                        Self.Tell(new ParseFeedItem(feed.FeedUri, item));
                    }
                    else
                    {
                        //Whoops, no HTML. We can mark this download as complete then.
                        Context.Parent.Tell(new FeedParserCoordinator.DownloadComplete(feed.FeedUri, 1, 0));
                    }
                }

                //No content in this feed. No need for further processing.
                if(feed.Items.Count == 0)
                    Context.Parent.Tell(new FeedParserCoordinator.EmptyFeed());
            });

            Receive<ParseFeedItem>(item =>
            {
                SendMessage(string.Format("Processing {0} for {1}", item.FeedItem.Link, item.FeedUri));

                //time to use the HMTL agility pack to process this content
                var doc = new HtmlDocument();
                doc.LoadHtml(item.FeedItem.Content);

                //find all of the IMG tags via XPATH
                var nodes = doc.DocumentNode.SelectNodes("//img[@src]");

                if (nodes != null)
                {
                    foreach (var imgNode in doc.DocumentNode.SelectNodes("//img[@src]"))
                    {
                        var imgUrl = imgNode.Attributes["src"].Value;

                        SendMessage(string.Format("Found image {0} inside {1}", imgUrl, item.FeedItem.Link));

                        //Let the coordinator know that we expect download results for moreimages...
                        Context.Parent.Tell(new FeedParserCoordinator.RemainingDownloadCount(item.FeedUri, 0, 1));

                        //And let the download actor know that it has work to do
                        _downloadActor.Tell(new HttpDownloaderActor.DownloadImage(item.FeedUri, imgUrl));
                    }
                }

                //Let the parent know that we've finished processing this HTML document
                Context.Parent.Tell(new FeedParserCoordinator.DownloadComplete(item.FeedUri, 1, 0));
                
            });
        }

        #region Messaging methods

        private void SendMessage(string message, PipeToSampleStatusCode pipeToSampleStatus = PipeToSampleStatusCode.Normal)
        {
            //create the message instance
            var consoleMsg = StatusMessageHelper.CreateMessage(message, pipeToSampleStatus);

            //Select the ConsoleWriterActor and send it a message
            Context.ActorSelection(_consoleWriterActorPath).Tell(consoleMsg);
        }

        #endregion
    }
}
