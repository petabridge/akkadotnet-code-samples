using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Routing;
using WebCrawler.Messages.State;

namespace WebCrawler.Service.Actors
{
    /// <summary>
    /// Actor responsible for managing the <see cref="DownloadsTracker"/> for each job.
    /// </summary>
    public class DownloadsMaster : ReceiveActor, WithUnboundedStash
    {
        public const string MasterBroadcastName = "broadcaster";

        #region Messages

        public class RequestDownloadTrackerFor
        {
            public RequestDownloadTrackerFor(CrawlJob key, ActorRef originator)
            {
                Originator = originator;
                Key = key;
            }

            public CrawlJob Key { get; private set; }

            public ActorRef Originator { get; private set; }
        }

        public class GetDownloadTracker
        {
            public GetDownloadTracker(CrawlJob key)
            {
                Key = key;
            }

            public CrawlJob Key { get; private set; }
        }

        public class TrackerNotFound
        {
            public TrackerNotFound(CrawlJob key)
            {
                Key = key;
            }

            public CrawlJob Key { get; private set; }
        }

        public class TrackerFound
        {
            public TrackerFound(CrawlJob key, ActorRef tracker)
            {
                Key = key;
                Tracker = tracker;
            }

            public CrawlJob Key { get; private set; }

            public ActorRef Tracker { get; private set; }
        }

        /// <summary>
        /// This tracker is dead - didn't receive a response back from it
        /// </summary>
        public class TrackerDead
        {
            public TrackerDead(CrawlJob key)
            {
                Key = key;
            }

            public CrawlJob Key { get; private set; }
        }

        public class CreatedTracker
        {
            public CreatedTracker(CrawlJob key, ActorRef tracker)
            {
                Tracker = tracker;
                Key = key;
            }

            public CrawlJob Key { get; private set; }

            public ActorRef Tracker { get; private set; }
        }

        #endregion

        /// <summary>
        /// The set of actors responsible for containing download state about a particular domain (defined by <see cref="CrawlJob"/>)
        /// </summary>
        protected Dictionary<CrawlJob, ActorRef> Trackers = new Dictionary<CrawlJob, ActorRef>();

        protected ActorRef MasterBroadcast;
        protected RequestDownloadTrackerFor RequestedTracker;

        protected int OutstandingAcknowledgments = 0;

        public DownloadsMaster()
        {
            Ready();
        }

        protected override void PreStart()
        {
            MasterBroadcast = Context.Child(MasterBroadcastName) == ActorRef.Nobody ? Context.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), MasterBroadcastName)
                : Context.Child(MasterBroadcastName);
        }

        protected override void PreRestart(System.Exception reason, object message)
        {
            /* don't kill the children */
            base.PostStop();
        }

        private void Ready()
        {
            Receive<RequestDownloadTrackerFor>(request =>
            {
                RequestedTracker = request;
                BecomeWaitingForTracker(request);
            });

            Receive<GetDownloadTracker>(get =>
            {
                HandleGetDownloadTracker(get);
            });

            Receive<TrackerFound>(found =>
            {
                Trackers[found.Key] = found.Tracker;
            });

            Receive<CreatedTracker>(tracker =>
            {
                Trackers[tracker.Key] = tracker.Tracker;
            });

            Receive<TrackerDead>(dead =>
            {
                if (Trackers.ContainsKey(dead.Key))
                {
                    Trackers.Remove(dead.Key);
                }
            });
        }



        private void BecomeWaitingForTracker(RequestDownloadTrackerFor request)
        {
            Become(WaitingForTracker);
            OutstandingAcknowledgments = MasterBroadcast.Ask<Routees>(new GetRoutees()).Result.Members.Count();
            MasterBroadcast.Tell(new GetDownloadTracker(request.Key));
            Context.SetReceiveTimeout(TimeSpan.FromSeconds(1.5));
        }

        private void WaitingForTracker()
        {
            //stash any further requests for trackers
            Receive<RequestDownloadTrackerFor>(request => Stash.Stash());

            Receive<ReceiveTimeout>(timeout => Self.Tell(new TrackerNotFound(RequestedTracker.Key)));

            Receive<TrackerNotFound>(notfound =>
            {
                // check to make sure that this broadcast is for the same job
                if (notfound.Key.Equals(RequestedTracker.Key))
                {
                    OutstandingAcknowledgments--;

                    //no one has a copy of this tracker, or it's dead
                    if (OutstandingAcknowledgments == 0)
                    {
                        var tracker = Context.ActorOf(Props.Create(() => new DownloadsTracker()),
                            Uri.EscapeUriString(RequestedTracker.Key.Root.ToString()));

                        MasterBroadcast.Tell(new CreatedTracker(RequestedTracker.Key, tracker));
                    }
                }
               
            });

            Receive<TrackerFound>(found =>
            {
                Trackers[found.Key] = found.Tracker;
                BecomeReadyIfFound(found);
            });

            Receive<CreatedTracker>(tracker =>
            {
                Trackers[tracker.Key] = tracker.Tracker;

                var found = new TrackerFound(tracker.Key, tracker.Tracker);
                BecomeReadyIfFound(found);
            });

            Receive<TrackerDead>(dead =>
            {
                if (Trackers.ContainsKey(dead.Key))
                {
                    Trackers.Remove(dead.Key);
                }
            });
        }

        private void BecomeReadyIfFound(TrackerFound found)
        {
            //check if this was for the job we're currently coordinating
            if (found.Key.Equals(RequestedTracker.Key))
            {
                RequestedTracker.Originator.Tell(found);
                Become(Ready);
                Stash.UnstashAll();
                Context.SetReceiveTimeout(null);
            }
        }

        public IStash Stash { get; set; }

        private void HandleGetDownloadTracker(GetDownloadTracker get)
        {
            // this tracker is a child of the current actor
            if (Context.Child(Uri.EscapeUriString(get.Key.ToString())) != ActorRef.Nobody)
            {
                var tracker = Context.Child(Uri.EscapeUriString(get.Key.ToString()));

                //let everyone know this tracker exists
                MasterBroadcast.Tell(new TrackerFound(get.Key, tracker));
            }
            else if (Trackers.ContainsKey(get.Key))
            {
                var tracker = Trackers[get.Key];

                //verfiy that this tracker is still alive
                tracker.Ask<ActorIdentity>(new Identify(get.Key), TimeSpan.FromSeconds(1.5))
                    .ContinueWith<object>(tr =>
                    {
                        if (tr.IsCanceled || tr.IsFaulted)
                            return new TrackerDead(get.Key);
                        return new TrackerFound(get.Key, tr.Result.Subject);
                    }).PipeTo(MasterBroadcast);
            }
            else
            {
                //otherwise, we couldn't find a definition for this tracker
                MasterBroadcast.Tell(new TrackerNotFound(get.Key));
            }
        }
    }
}
