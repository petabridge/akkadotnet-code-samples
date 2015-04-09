using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Routing;
using WebCrawler.Messages.Commands;
using WebCrawler.Messages.State;
using WebCrawler.Service.Actors.Downloads;
using WebCrawler.Service.State;

namespace WebCrawler.Service.Actors.IO
{
    /// <summary>
    /// Actor responsible for individual <see cref="CrawlJob"/>
    /// </summary>
    public class CrawlMaster : ReceiveActor, IWithUnboundedStash
    {
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

            Receive<SubscribeToJob>(subscribe =>
            {
                if(subscribe.Job.Equals(Job))
                    Subscribers.Add(subscribe.Subscriber);
            });

            Receive<UnsubscribeFromJob>(unsubscribe =>
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
            Receive<StartJob>(start =>
            {
                Subscribers.Add(start.Requestor);
                var downloadRootDocument = new DownloadWorker.DownloadHtmlDocument(new CrawlDocument(start.Job.Root));

                //should kick off the initial downloads and parsing
                CoordinatorRouter.Tell(downloadRootDocument);

                Become(Started);
                Stash.UnstashAll();
            });

            ReceiveAny(o => Stash.Stash());
          
        }

        private void Started()
        {
            Receive<StartJob>(start =>
            {
                //treat the additional StartJob like a subscription
                if (start.Job.Equals(Job))
                    Subscribers.Add(start.Requestor);
            });

            Receive<SubscribeToJob>(subscribe =>
            {
                if (subscribe.Job.Equals(Job))
                    Subscribers.Add(subscribe.Subscriber);
            });

            Receive<UnsubscribeFromJob>(unsubscribe =>
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
