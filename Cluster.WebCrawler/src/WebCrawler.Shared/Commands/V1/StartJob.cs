// -----------------------------------------------------------------------
// <copyright file="StartJob.cs" company="Petabridge, LLC">
//      Copyright (C) 2018 - 2018 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using WebCrawler.Shared.State;

namespace WebCrawler.Shared.Commands.V1
{
    /// <summary>
    ///     Launch a new <see cref="CrawlJob" />
    /// </summary>
    public class StartJob : IStartJobV1
    {
        public StartJob(CrawlJob job, IActorRef requestor)
        {
            Requestor = requestor;
            Job = job;
        }

        public CrawlJob Job { get; }

        public IActorRef Requestor { get; }
        public object ConsistentHashKey => Job.Root.OriginalString;
    }

    /// <summary>
    ///     Kill a running <see cref="CrawlJob" />
    /// </summary>
    public class StopJob
    {
        public StopJob(CrawlJob job, IActorRef requestor)
        {
            Requestor = requestor;
            Job = job;
        }

        public CrawlJob Job { get; }

        public IActorRef Requestor { get; }
    }
}