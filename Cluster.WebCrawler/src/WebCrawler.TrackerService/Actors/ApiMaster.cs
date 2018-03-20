// -----------------------------------------------------------------------
// <copyright file="ApiMaster.cs" company="Petabridge, LLC">
//      Copyright (C) 2018 - 2018 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Actor;
using Akka.Routing;
using WebCrawler.Shared.Commands.V1;
using WebCrawler.Shared.State;
using WebCrawler.TrackerService.Actors.IO;
using WebCrawler.TrackerService.Actors.Tracking;

namespace WebCrawler.TrackerService.Actors
{
    /// <summary>
    ///     The very top-level actor. Oversees all requests from front-end machines.
    /// </summary>
    public class ApiMaster : ReceiveActor, IWithUnboundedStash
    {
        public const string MasterBroadcastName = "broadcaster";

        protected IActorRef ApiBroadcaster;
        protected IStartJobV1 JobToStart;
        protected int OutstandingAcknowledgements;

        public ApiMaster()
        {
            Ready();
        }

        public IStash Stash { get; set; }

        protected override void PreStart()
        {
            ApiBroadcaster = Context.Child(MasterBroadcastName).Equals(ActorRefs.Nobody)
                ? Context.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), MasterBroadcastName)
                : Context.Child(MasterBroadcastName);
        }

        protected override void PreRestart(Exception reason, object message)
        {
            /* Don't kill the children */
            PostStop();
        }

        private void Ready()
        {
            Receive<IStartJobV1>(start =>
            {
                JobToStart = start;

                // contact all of our peers and see if this job is already running
                ApiBroadcaster.Tell(new FindRunningJob(start.Job));

                // and see if we've done this job before
                Context.ActorSelection("/user/downloads")
                    .Tell(new DownloadsMaster.RequestDownloadTrackerFor(start.Job, Self));

                Become(SearchingForJob);
                var members = ApiBroadcaster.Ask<Routees>(new GetRoutees()).Result.Members.ToList();
                OutstandingAcknowledgements = members.Count();
                Context.SetReceiveTimeout(TimeSpan.FromSeconds(3.0));
            });

            Receive<FindRunningJob>(job => HandleFindRunningJob(job));
        }

        private void SearchingForJob()
        {
            //not able to start more jobs right now
            Receive<IStartJobV1>(s => Stash.Stash());

            Receive<FindRunningJob>(job => HandleFindRunningJob(job));

            Receive<JobFound>(jobFound =>
            {
                if (jobFound.Key.Equals(JobToStart.Job))
                {
                    jobFound.CrawlMaster.Tell(new SubscribeToJob(JobToStart.Job, JobToStart.Requestor));
                    BecomeReady();
                }
            });

            //treat as a "not found" message
            Receive<ReceiveTimeout>(timeout => ApiBroadcaster.Tell(new JobNotFound(JobToStart.Job)));

            Receive<JobNotFound>(notFound =>
            {
                if (notFound.Key.Equals(JobToStart.Job))
                {
                    OutstandingAcknowledgements--;
                    if (OutstandingAcknowledgements <= 0)
                    {
                        //need to create the job now
                        var crawlMaster = Context.ActorOf(Props.Create(() => new CrawlMaster(JobToStart.Job)),
                            JobToStart.Job.Root.ToActorName());
                        crawlMaster.Tell(JobToStart);
                        ApiBroadcaster.Tell(new JobFound(JobToStart.Job, crawlMaster));
                    }
                }
            });
        }

        private void BecomeReady()
        {
            Become(Ready);
            Context.SetReceiveTimeout(null);
            Stash.UnstashAll();
        }

        private void HandleFindRunningJob(FindRunningJob job)
        {
            var haveChild = Context.Child(job.Key.Root.ToActorName());

            //found a running job already
            if (!haveChild.Equals(ActorRefs.Nobody))
            {
                ApiBroadcaster.Tell(new JobFound(job.Key, haveChild));
                return;
            }

            ApiBroadcaster.Tell(new JobNotFound(job.Key));
        }

        #region Messages

        public class FindRunningJob
        {
            public FindRunningJob(CrawlJob key)
            {
                Key = key;
            }

            public CrawlJob Key { get; }
        }

        public class JobFound
        {
            public JobFound(CrawlJob key, IActorRef crawlMaster)
            {
                CrawlMaster = crawlMaster;
                Key = key;
            }

            public CrawlJob Key { get; }

            public IActorRef CrawlMaster { get; }
        }

        public class JobNotFound
        {
            public JobNotFound(CrawlJob key)
            {
                Key = key;
            }

            public CrawlJob Key { get; }
        }

        #endregion
    }
}