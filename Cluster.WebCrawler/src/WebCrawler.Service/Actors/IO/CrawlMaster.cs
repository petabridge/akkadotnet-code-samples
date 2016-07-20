using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Routing;
using WebCrawler.Messages.Commands;
using WebCrawler.Messages.Commands.V1;
using WebCrawler.Messages.State;
using WebCrawler.Shared.IO;
using WebCrawler.TrackingService.Actors.Downloads;
using WebCrawler.TrackingService.State;

namespace WebCrawler.TrackingService.Actors.IO
{
    /// <summary>
    /// Actor responsible for individual <see cref="CrawlJob"/>
    /// </summary>
    public class CrawlMaster : ReceiveActor, IWithUnboundedStash
    {

        #region Messages

        public class CrawlCanStart
        {
            public CrawlCanStart(IStartJobV1 job, int nodeCount)
            {
                Job = job;
                NodeCount = nodeCount;
            }

            public IStartJobV1 Job { get; private set; }
            public int NodeCount { get; private set; }
        }

        public class AttemptToStartJob
        {
            public AttemptToStartJob(IStartJobV1 job)
            {
                Job = job;
            }

            public IStartJobV1 Job { get; private set; }
        }

        #endregion

        public const string CoordinatorRouterName = "coordinators";
        protected readonly CrawlJob Job;

        /// <summary>
        /// All of the actors subscribed to updates for <see cref="Job"/>
        /// </summary>
        protected HashSet<IActorRef> Subscribers = new HashSet<IActorRef>();

        protected JobStatusUpdate RunningStatus;

        protected CrawlJobStats TotalStats
        {
            get { return RunningStatus.Stats; }
            set { RunningStatus.Stats = value; }
        }

        protected IActorRef CoordinatorRouter;
        protected IActorRef DownloadTracker;
        protected ICancelable JobStarter;
        protected ILoggingAdapter Log = Context.GetLogger();

        public IStash Stash { get; set; }

        public CrawlMaster(CrawlJob job)
        {
            Job = job;
            RunningStatus = new JobStatusUpdate(Job);
            TotalStats = new CrawlJobStats(Job);
            Context.SetReceiveTimeout(TimeSpan.FromSeconds(5));
            WaitingForTracker();
        }

        protected override void PreStart()
        {
            /* Request a download tracker instance from the downloads master */
            Context.ActorSelection("/user/downloads").Tell(new DownloadsMaster.RequestDownloadTrackerFor(Job, Self));
        }

        private void WaitingForTracker()
        {
            //job failed to start
            Receive<ReceiveTimeout>(timeout => EndJob(JobStatus.Failed));

            Receive<ISubscribeToJobV1>(subscribe =>
            {
                if (subscribe.Job.Equals(Job))
                    Subscribers.Add(subscribe.Subscriber);
            });

            Receive<IUnsubscribeFromJobV1>(unsubscribe =>
            {
                if (unsubscribe.Job.Equals(Job))
                    Subscribers.Remove(unsubscribe.Subscriber);
            });

            Receive<DownloadsMaster.TrackerFound>(tr =>
            {
                DownloadTracker = tr.Tracker;
                BecomeReady();
            });

            // stash everything else until we have a tracker
            ReceiveAny(o => Stash.Stash());
        }
        private void BecomeReady()
        {
            if (Context.Child(CoordinatorRouterName).Equals(ActorRefs.Nobody))
            {
                CoordinatorRouter =
                    Context.ActorOf(
                        Props.Create(() => new DownloadCoordinator(Job, Self, DownloadTracker, 50))
                            .WithRouter(FromConfig.Instance), CoordinatorRouterName);
            }
            else //in the event of a restart
            {
                CoordinatorRouter = Context.Child(CoordinatorRouterName);
            }
            Become(Ready);
            Stash.UnstashAll();
            Context.SetReceiveTimeout(TimeSpan.FromSeconds(120));
        }

        private void Ready()
        {
            // kick off the job
            Receive<IStartJobV1>(start =>
            {
                Subscribers.Add(start.Requestor);

                JobStarter = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(TimeSpan.FromMilliseconds(20),
                    TimeSpan.FromMilliseconds(20), Self, new AttemptToStartJob(start), Self);
            });

            Receive<AttemptToStartJob>(start =>
            {
                var self = Self;
                CoordinatorRouter.Ask<Routees>(new GetRoutees()).ContinueWith(tr =>
                {
                    return new CrawlCanStart(start.Job, tr.Result.Members.Count());
                }).PipeTo(self);
            });

            Receive<CrawlCanStart>(start => start.NodeCount > 0, start =>
            {
                var downloadRootDocument = new DownloadHtmlDocument(new CrawlDocument(start.Job.Job.Root));

                //should kick off the initial downloads and parsing
                //var routees = CoordinatorRouter.Ask<Routees>(new GetRoutees()).Result;
                CoordinatorRouter.Tell(downloadRootDocument);
                JobStarter.Cancel();

                Become(Started);
                Stash.UnstashAll();
            });

            Receive<CrawlCanStart>(start =>
            {
                Log.Info("Can't start job yet. No routees.");
            });


            ReceiveAny(o => Stash.Stash());

        }

        private void Started()
        {
            Receive<IStartJobV1>(start =>
            {
                //treat the additional StartJob like a subscription
                if (start.Job.Equals(Job))
                    Subscribers.Add(start.Requestor);
            });

            Receive<ISubscribeToJobV1>(subscribe =>
            {
                if (subscribe.Job.Equals(Job))
                    Subscribers.Add(subscribe.Subscriber);
            });

            Receive<IUnsubscribeFromJobV1>(unsubscribe =>
            {
                if (unsubscribe.Job.Equals(Job))
                    Subscribers.Remove(unsubscribe.Subscriber);
            });

            Receive<CrawlJobStats>(stats =>
            {
                TotalStats = TotalStats.Merge(stats);
                PublishJobStatus();
            });

            Receive<StopJob>(stop => EndJob(JobStatus.Stopped));

            //job is finished
            Receive<ReceiveTimeout>(timeout => EndJob(JobStatus.Finished));
        }

        private void EndJob(JobStatus finalStatus)
        {
            RunningStatus.Status = finalStatus;
            RunningStatus.EndTime = DateTime.UtcNow;
            PublishJobStatus();
            Self.Tell(PoisonPill.Instance);
        }


        private void PublishJobStatus()
        {
            foreach (var sub in Subscribers)
                sub.Tell(RunningStatus);
        }
    }
}
