using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using HtmlAgilityPack;
using WebCrawler.Messages.State;
using WebCrawler.Shared.IO.Messages;
using WebCrawler.TrackingService.State;

namespace WebCrawler.Shared.IO
{
    /// <summary>
    /// Actor responsibile for using the HTML Agility Pack to parse links to other content
    /// and images out of HTML documents
    /// </summary>
    public class ParseWorker : ReceiveActor, IWithUnboundedStash
    {
        #region Messages

        /// <summary>
        /// Allows us to change the <see cref="ParseWorker.DownloadActor"/> for this <see cref="ParseWorker"/>.
        /// </summary>
        public class SetDownloadActor
        {
            public SetDownloadActor(IActorRef downloader)
            {
                Downloader = downloader;
            }

            public IActorRef Downloader { get; private set; }
        }

        /// <summary>
        /// Requests a <see cref="ParseWorker.SetDownloadActor"/> response from the parent
        /// </summary>
        public class RequestDownloadActor { }

        #endregion

        /// <summary>
        /// Need to use this to determine which URLs share the same domain
        /// </summary>
        protected readonly CrawlJob JobRoot;

        protected readonly IActorRef CoordinatorActor;
        protected IActorRef DownloadActor;

        public IStash Stash { get; set; }

        public ParseWorker(CrawlJob jobRoot, IActorRef coordinatorActor)
        {
            JobRoot = jobRoot;
            CoordinatorActor = coordinatorActor;
            WaitingForDownloadActor();
        }

        protected override void PreStart()
        {
            CoordinatorActor.Tell(new RequestDownloadActor());
        }

        protected override void PreRestart(Exception reason, object message)
        {
            //save whatever is in the stash
            Stash.UnstashAll();
            base.PreRestart(reason, message);
        }

        private void WaitingForDownloadActor()
        {
            Receive<SetDownloadActor>(download =>
            {
                DownloadActor = download.Downloader;
                BecomeParsing();
            });

            //stash all messages until we've received a reference to the DownloadActor
            ReceiveAny(o => Stash.Stash());
        }

        private void BecomeParsing()
        {
            Become(Parsing);
            Stash.UnstashAll();
        }

        private void Parsing()
        {
            Receive<DownloadHtmlResult>(downloadHtmlResult =>
            {
                var requestedUrls = new List<CrawlDocument>();

                var htmlString = downloadHtmlResult.Content;
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlString);

                //find all of the IMG tags via XPATH
                var imgs = doc.DocumentNode.SelectNodes("//img[@src]");

                //find all of the A...HREF tags via XPATH
                var links = doc.DocumentNode.SelectNodes("//a[@href]");

                /* PROCESS ALL IMAGES */
                if (imgs != null)
                {
                    var validImgUris =
                        imgs.Select(x => x.Attributes["src"].Value)
                            .Where(CanMakeAbsoluteUri)
                            .Select(ToAsboluteUri)
                            .Where(AbsoluteUriIsInDomain)
                            .Select(y => new CrawlDocument(y, true));

                    requestedUrls = requestedUrls.Concat(validImgUris).ToList();
                }

                /* PROCESS ALL LINKS */
                if (links != null)
                {
                    var validLinkUris =
                        links.Select(x => x.Attributes["href"].Value)
                            .Where(CanMakeAbsoluteUri)
                            .Select(ToAsboluteUri)
                            .Where(AbsoluteUriIsInDomain)
                            .Select(y => new CrawlDocument(y, false));

                    requestedUrls = requestedUrls.Concat(validLinkUris).ToList();
                }

                // TODO: replace with some code that uses an estimated weighted moving average based on some combined query stats
                // (probably an exercise for a different day)
                CoordinatorActor.Tell(new CheckDocuments(requestedUrls, DownloadActor, TimeSpan.FromMilliseconds(requestedUrls.Count * 5000)), Self);
            });

            Receive<SetDownloadActor>(download =>
            {
                DownloadActor = download.Downloader;
            });
        }

        #region URI formatting tools

        public bool CanMakeAbsoluteUri(string rawUri)
        {
            if (Uri.IsWellFormedUriString(rawUri, UriKind.Absolute))
                return true;
            try
            {
                var absUri = new Uri(JobRoot.Root, rawUri);
                var returnVal = absUri.Scheme.Equals(Uri.UriSchemeHttp) || absUri.Scheme.Equals(Uri.UriSchemeHttps);
                return returnVal;
            }
            catch
            {
                return false;
            }
        }

        public bool AbsoluteUriIsInDomain(Uri otherUri)
        {
            return JobRoot.Domain == otherUri.Host;
        }

        public Uri ToAsboluteUri(string rawUri)
        {
            return Uri.IsWellFormedUriString(rawUri, UriKind.Absolute) ? new Uri(rawUri, UriKind.Absolute) : new Uri(JobRoot.Root, rawUri);
        }

        #endregion
    }
}
