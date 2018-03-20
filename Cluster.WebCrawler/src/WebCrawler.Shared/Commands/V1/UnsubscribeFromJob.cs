﻿using Akka.Actor;
using WebCrawler.Shared.State;

namespace WebCrawler.Shared.Commands.V1
{
    /// <summary>
    /// Unsuscribe an actor from a given <see cref="CrawlJob"/>
    /// </summary>
    public class UnsubscribeFromJob : IUnsubscribeFromJobV1
    {
        public UnsubscribeFromJob(CrawlJob job, IActorRef subscriber)
        {
            Subscriber = subscriber;
            Job = job;
        }

        public CrawlJob Job { get; private set; }

        public IActorRef Subscriber { get; private set; }
    }
}