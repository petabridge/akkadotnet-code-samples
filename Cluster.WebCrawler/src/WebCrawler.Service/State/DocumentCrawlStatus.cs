using System;
using Akka.Actor;
using Helios.Util.TimedOps;

namespace WebCrawler.Service.State
{
    /// <summary>
    /// The status of a particular <see cref="CrawlDocument"/> operation.
    /// 
    /// If the crawl operatoin isn't completed before the elapsed time, another actor can start
    /// the process.
    /// </summary>
    public class DocumentCrawlStatus
    {
        public DocumentCrawlStatus(CrawlDocument document, Deadline timeout)
        {
            Timeout = timeout;
            Document = document;
        }

        public CrawlDocument Document { get; private set; }

        public bool IsComplete { get; private set; }

        public Deadline Timeout { get; private set; }

        public bool CanProcess
        {
            get { return !IsComplete && (Timeout == null || Timeout.IsOverdue); }
        }

        public ActorRef Owner { get; private set; }

        public DocumentCrawlStatus MarkAsComplete()
        {
            IsComplete = true;
            Timeout = null; //let it get GC'ed
            Owner = null; //doesn't matter
            return this;
        }

        public bool TryClaim(ActorRef newOwner, TimeSpan crawlTime)
        {
            if (CanProcess)
            {
                Timeout = Deadline.Now + crawlTime;
                Owner = newOwner;
                return true;
            }
            return false;
        }
    }
}
