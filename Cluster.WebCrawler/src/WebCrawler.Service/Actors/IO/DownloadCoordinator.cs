using System;
using System.Linq;
using System.Threading;
using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using WebCrawler.Messages.State;
using WebCrawler.Service.Messages;

namespace WebCrawler.Service.Actors.IO
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

        protected readonly IActorRef DownloadsTracker;
        protected readonly IActorRef Commander;

        protected IActorRef DownloaderRouter;
        protected IActorRef ParserRouter;

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
                DownloadsTracker.Tell(documents);
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
                    // Context.Parent is the router between the coordinators and the Commander
                    if (doc.IsImage)
                    {
                        Context.Parent.Tell(new DownloadWorker.DownloadImage(doc));
                    }
                    else
                    {
                        Context.Parent.Tell(new DownloadWorker.DownloadHtmlDocument(doc));
                    }
                }
            });

            //hand the work off to the downloaders
            Receive<DownloadWorker.IDownloadDocument>(download =>
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
