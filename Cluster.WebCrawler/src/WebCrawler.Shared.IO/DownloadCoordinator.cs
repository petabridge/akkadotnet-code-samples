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

namespace WebCrawler.Shared.IO
{
    /// <summary>
    /// Actor responsible for using Akka.Streams to execute download and parsing of all content.
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

        public class StreamCompleteTick
        {
            private StreamCompleteTick() { }
            public static readonly StreamCompleteTick Instance = new StreamCompleteTick();
        }

        #endregion

        const int DefaultMaxConcurrentDownloads = 50;
        protected readonly IActorRef DownloadsTracker;
        protected readonly IActorRef Commander;

        protected IActorRef DownloaderRouter;
        protected IActorRef ParserRouter;
        protected IActorRef SourceActor;

        protected CrawlJob Job;
        protected CrawlJobStats Stats;

        protected readonly long MaxConcurrentDownloads;

        private ICancelable _publishStatsTask;

        private ILoggingAdapter _logger = Context.GetLogger();

        public DownloadCoordinator(CrawlJob job, IActorRef commander, IActorRef downloadsTracker, long maxConcurrentDownloads)
        {
            Job = job;
            DownloadsTracker = downloadsTracker;
            MaxConcurrentDownloads = maxConcurrentDownloads;
            Commander = commander;
            Stats = new CrawlJobStats(Job);
            var selfHtmlSink = Sink.ActorRef<CheckDocuments>(Self, StreamCompleteTick.Instance);
            var selfDocSink = Sink.ActorRef<CompletedDocument>(Self, StreamCompleteTick.Instance);
            var selfImgSink = Sink.ActorRef<CompletedDocument>(Self, StreamCompleteTick.Instance);
            var htmlFlow = Flow.Create<CrawlDocument>().Via(DownloadFlow.SelectDocType())
                .Throttle(30, TimeSpan.FromSeconds(1), 100, ThrottleMode.Shaping)
                .Via(DownloadFlow.ProcessHtmlDownloadFor(DefaultMaxConcurrentDownloads, HttpClientFactory.GetClient()));

            var imageFlow = Flow.Create<CrawlDocument>()
                .Via(DownloadFlow.SelectDocType())
                .Throttle(30, TimeSpan.FromSeconds(1), 100, ThrottleMode.Shaping)
                .Via(DownloadFlow.ProcessImageDownloadFor(DefaultMaxConcurrentDownloads, HttpClientFactory.GetClient()))
                .Via(DownloadFlow.ProcessCompletedDownload());

            var source = Source.ActorRef<CrawlDocument>(5000, OverflowStrategy.DropTail);

           var graph = GraphDsl.Create(source, (builder, s) =>
            {
                // html flows
                var downloadHtmlFlow = builder.Add(htmlFlow);
                var downloadBroadcast = builder.Add(new Broadcast<DownloadHtmlResult>(2));
                var completedDownload = builder.Add(DownloadFlow.ProcessCompletedHtmlDownload());
                var parseCompletedDownload = builder.Add(ParseFlow.GetParseFlow(Job));
                var htmlSink = builder.Add(selfHtmlSink);
                var docSink = builder.Add(selfDocSink);
                builder.From(downloadHtmlFlow).To(downloadBroadcast);
                builder.From(downloadBroadcast.Out(0)).To(completedDownload.Inlet);
                builder.From(downloadBroadcast.Out(1)).To(parseCompletedDownload.Inlet);
                builder.From(parseCompletedDownload).To(htmlSink);
                builder.From(completedDownload).To(docSink);

                // image flows
                var imgSink = builder.Add(selfImgSink);
                var downloadImageFlow = builder.Add(imageFlow);
                builder.From(downloadImageFlow).To(imgSink);

                var sourceBroadcast = builder.Add(new Broadcast<CrawlDocument>(2));
                builder.From(sourceBroadcast.Out(0)).To(downloadImageFlow.Inlet);
                builder.From(sourceBroadcast.Out(1)).To(downloadHtmlFlow.Inlet);

                builder.From(s.Outlet).To(sourceBroadcast.In);

                return ClosedShape.Instance;
            });

            SourceActor = Context.Materializer().Materialize(graph);

            Receiving();
        }

        protected override void PreStart()
        {
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
                //forward this onto the downloads tracker, but have it reply back to our parent router so the work might get distributed more evenly
                DownloadsTracker.Tell(documents, Context.Parent);
            });

            //Update our local stats
            Receive<DiscoveredDocuments>(discovered =>
            {
                Stats = Stats.WithDiscovered(discovered);
            });

            //Received word from the DownloadTracker that we need to process some docs
            Receive<ProcessDocuments>(process =>
            {
                foreach (var doc in process.Documents)
                {
                    SourceActor.Tell(doc);
                }
            });

            //hand the work off to the downloaders
            Receive<IDownloadDocument>(download =>
            {
                SourceActor.Tell(download.Document);
            });

            Receive<CompletedDocument>(completed =>
            {
                _logger.Info("Logging completed download {0} bytes {1}", completed.Document.DocumentUri,completed.NumBytes);
                Stats = Stats.WithCompleted(completed);
                _logger.Info("Total stats {0}", Stats);
            });

            Receive<StreamCompleteTick>(_ =>
            {
                _logger.Info("Stream has completed. No more messages to process.");
            });
        }
    }
}
