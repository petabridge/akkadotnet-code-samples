using System;
using System.Collections.Generic;
using System.Net.Http;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using Akka.Streams;
using Akka.Streams.Actors;
using Akka.Streams.Dsl;
using Akka.Streams.Implementation;
using WebCrawler.Messages.State;
using WebCrawler.Shared.IO.Messages;
using WebCrawler.TrackingService.State;
using ActorRefImplicitSenderExtensions = Akka.Actor.ActorRefImplicitSenderExtensions;

namespace WebCrawler.Shared.IO
{
    /// <summary>
    /// Actor responsible for managing pool of <see cref="DownloadWorker"/> and <see cref="ParseWorker"/> actors.
    /// 
    /// Can be remote-deployed to other systems.
    /// 
    /// Publishes statistics updates to its parent.
    /// </summary>
    public class DownloadCoordinator : ReceiveActor
    {
        #region Constants

        public const string Downloader = "downloader";
        public const string Parser = "parser";

        #endregion

        #region Messages

        /// <summary>
        /// Used to signal that it's time to publish to the JobMaster
        /// </summary>
        public class PublishStatsTick
        {
            private PublishStatsTick() { }
            private static readonly PublishStatsTick _instance = new PublishStatsTick();

            public static PublishStatsTick Instance
            {
                get { return _instance; }
            }
        }

        #endregion

        const int DefaultMaxConcurrentDownloads = 50;
        protected readonly IActorRef DownloadsTracker;
        protected readonly IActorRef Commander;

        protected IActorRef DownloaderRouter;
        protected IActorRef ParserRouter;

        protected CrawlJob Job;
        protected CrawlJobStats Stats;

        protected readonly long MaxConcurrentDownloads;

        private ICancelable _publishStatsTask;

        private ILoggingAdapter _logger = Context.GetLogger();

        private Sink<CheckDocuments, NotUsed> _selfHtmlSink;
        private Sink<CompletedDocument, NotUsed> _selfDocSink;
        private Flow<CrawlDocument, CompletedDocument, NotUsed> _downloadImageFlow;
        private Flow<CrawlDocument, DownloadHtmlResult, NotUsed> _downloadHtmlFlow;

        public DownloadCoordinator(CrawlJob job, IActorRef commander, IActorRef downloadsTracker, long maxConcurrentDownloads)
        {
            Job = job;
            DownloadsTracker = downloadsTracker;
            MaxConcurrentDownloads = maxConcurrentDownloads;
            Commander = commander;
            Stats = new CrawlJobStats(Job);
            _selfHtmlSink = Sink.ActorRef<CheckDocuments>(Self, PublishStatsTick.Instance);
            _selfDocSink = Sink.ActorRef<CompletedDocument>(Self, PublishStatsTick.Instance);
            _downloadHtmlFlow = Flow.Create<CrawlDocument>().Via(DownloadFlow.SelectDocType())
                .Buffer(10, OverflowStrategy.Backpressure)
                .Via(DownloadFlow.ProcessHtmlDownloadFor(DefaultMaxConcurrentDownloads, HttpClientFactory.GetClient()));

            _downloadImageFlow = Flow.Create<CrawlDocument>()
                .Via(DownloadFlow.SelectDocType())
                .Buffer(10, OverflowStrategy.Backpressure)
                .Via(DownloadFlow.ProcessImageDownloadFor(DefaultMaxConcurrentDownloads, HttpClientFactory.GetClient()))
                .Via(DownloadFlow.ProcessCompletedDownload());

            Receiving();
        }

        protected override void PreStart()
        {

            // Create our downloader pool
            if (Context.Child(Downloader).Equals(ActorRefs.Nobody))
            {
                DownloaderRouter = Context.ActorOf(
                    Props.Create(() => new DownloadWorker(HttpClientFactory.GetClient, Self, (int)MaxConcurrentDownloads)).WithRouter(new RoundRobinPool(10)),
                    Downloader);
            }

            // Create our parser pool
            if (Context.Child(Parser).Equals(ActorRefs.Nobody))
            {
                ParserRouter = Context.ActorOf(
                    Props.Create(() => new ParseWorker(Job, Self)).WithRouter(new RoundRobinPool(10)),
                    Parser);
            }

            // Schedule regular stats updates
            _publishStatsTask = new Cancelable(Context.System.Scheduler);
            Context.System.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(250), Self, PublishStatsTick.Instance, Self, _publishStatsTask);
        }

        protected override void PreRestart(Exception reason, object message)
        {
            //don't dispose of children
            PostStop();
        }

        protected override void PostStop()
        {
            try
            {
                //cancel the regularly scheduled task
                _publishStatsTask.Cancel();
            }
            catch { }
        }

        private void Receiving()
        {
            Receive<PublishStatsTick>(stats =>
            {
                if (!Stats.IsEmpty)
                {
                    _logger.Info("Publishing {0} to parent", Stats);

                    Commander.Tell(Stats.Copy());

                    //reset our stats after publishing
                    Stats = Stats.Reset();
                }
            });

            //Received word from a ParseWorker that we need to check for new documents
            Receive<CheckDocuments>(documents =>
            {
                //forward this onto the downloads tracker, but have it reply back to us
                ((ICanTell)DownloadsTracker).Tell(documents, Self);
            });

            //Update our local stats
            Receive<DiscoveredDocuments>(discovered =>
            {
                Stats = Stats.WithDiscovered(discovered);
            });

            //Received word from the DownloadTracker that we need to process some docs
            Receive<ProcessDocuments>(process =>
            {
                var s = Source.From(process.Documents);

                _downloadHtmlFlow.RunWith(s, _selfHtmlSink, Context.Materializer());
                _downloadImageFlow.RunWith(s, _selfDocSink, Context.Materializer());
            });

            //hand the work off to the downloaders
            Receive<IDownloadDocument>(download =>
            {
                DownloaderRouter.Tell(download);
            });

            Receive<CompletedDocument>(completed =>
            {
                //TODO: send verbose status messages to commander here?
                Stats = Stats.WithCompleted(completed);
            });

            /* Set all of our local downloaders to message our local parsers */
            Receive<DownloadWorker.RequestParseActor>(request =>
            {
                Sender.Tell(new DownloadWorker.SetParseActor(ParserRouter));
            });

            /* Set all of our local parsers to message our local downloaders */
            Receive<ParseWorker.RequestDownloadActor>(request =>
            {
                Sender.Tell(new ParseWorker.SetDownloadActor(DownloaderRouter));
            });
        }
    }
}
