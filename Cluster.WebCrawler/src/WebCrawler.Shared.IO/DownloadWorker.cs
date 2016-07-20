using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Akka.Actor;
using WebCrawler.Messages.State;
using WebCrawler.TrackingService.State;

namespace WebCrawler.Shared.IO
{
    /// <summary>
    /// Actor responsible for using <see cref="System.Net.Http.HttpClient"/>
    /// </summary>
    public class DownloadWorker : ReceiveActor, IWithUnboundedStash
    {
        #region Messages

        /// <summary>
        /// Allows us to change the <see cref="ParseActor"/> for this <see cref="DownloadWorker"/>.
        /// </summary>
        public class SetParseActor
        {
            public SetParseActor(IActorRef parser)
            {
                Parser = parser;
            }

            public IActorRef Parser { get; private set; }
        }

        /// <summary>
        /// Requests a <see cref="SetParseActor"/> response from the parent
        /// </summary>
        public class RequestParseActor { }

        #endregion

        private HttpClient _httpClient;
        private readonly Func<HttpClient> _httpClientFactory;

        /// <summary>
        /// Default number of maximum current operations per <see cref="DownloadWorker"/>
        /// </summary>
        public const int DefaultMaxConcurrentDownloads = 50;

        protected int MaxConcurrentDownloads;

        protected int CurrentDownloadCount
        {
            get { return _currentDownloads.Count; }
        }

        /// <summary>
        /// Returns true if we can accept new downloads - otherwise we have to stash the message
        /// </summary>
        protected bool CanDoDownload
        {
            get { return CurrentDownloadCount < MaxConcurrentDownloads; }
        }

        protected readonly IActorRef CoordinatorActor;
        protected IActorRef ParseActor;

        private readonly HashSet<IDownloadDocument> _currentDownloads = new HashSet<IDownloadDocument>();

        public DownloadWorker(Func<HttpClient> httpClientFactory, IActorRef coordinatorActor, int maxConcurrentDownloads = DefaultMaxConcurrentDownloads)
        {
            _httpClientFactory = httpClientFactory;
            MaxConcurrentDownloads = maxConcurrentDownloads;
            Debug.Assert(maxConcurrentDownloads > 0, "maxConcurrentDownloads must be greater than 0");
            CoordinatorActor = coordinatorActor;
            WaitingForParseActor();
        }

        public IStash Stash { get; set; }

        protected override void PreStart()
        {
            //create a new HttpClient instance
            _httpClient = _httpClientFactory();

            //ask for a reference to the parse actor
            CoordinatorActor.Tell(new RequestParseActor());
        }

        protected override void PostStop()
        {
            try
            {
                if (_httpClient != null)
                {
                    // dispose the HttpClient
                    // ignore disposal exceptions
                    _httpClient.Dispose();
                }
            }
            catch { }
            base.PostStop();
        }

        protected override void PreRestart(Exception reason, object message)
        {
            // empty the stash back into our mailbox before we restart
            Stash.UnstashAll();
            base.PreRestart(reason, message);
        }

        protected override void PostRestart(Exception reason)
        {
            Become(WaitingForParseActor);
        }

        private void WaitingForParseActor()
        {
            Receive<SetParseActor>(parse =>
            {
                ParseActor = parse.Parser;
                Become(Ready);
                Stash.UnstashAll();
            });

            ReceiveAny(o => Stash.Stash());
        }

        /// <summary>
        /// Behavior for when <see cref="CurrentDownloadCount"/> is less than <see cref="MaxConcurrentDownloads"/>
        /// </summary>
        private void Ready()
        {
            Receive<DownloadHtmlDocument>(i => CanDoDownload, html =>
            {
                if (!(Uri.UriSchemeHttp.Equals(html.Document.DocumentUri.Scheme) ||
                    Uri.UriSchemeHttps.Equals(html.Document.DocumentUri.Scheme)))
                    return;

                 _currentDownloads.Add(html);
                PipeToSupport.PipeTo<DownloadHtmlResult>(_httpClient.GetStringAsync(html.Document.DocumentUri).ContinueWith(ContinuationFunction(html)), Self);
            });

            Receive<DownloadImage>(i => CanDoDownload, image =>
            {
                _currentDownloads.Add(image);
                PipeToSupport.PipeTo<DownloadImageResult>(_httpClient.GetByteArrayAsync(image.Document.DocumentUri).ContinueWith(DownloadImageContinuationFunction(image)), Self);
            });

            // When we've hit maximum # of concurrent downloads
            Receive<IDownloadDocument>(i => !CanDoDownload, document =>
            {
                /* Stash any additional job requests and switch behaviors */
                Stash.Stash();
                Become(WaitingForDownloads);
            });

            Receive<DownloadImageResult>(image => HandleImageDownload(image));

            Receive<DownloadHtmlResult>(html => HandleHtmlDownload(html));

            Receive<SetParseActor>(parse =>
            {
                ParseActor = parse.Parser;
            });
        }

        private static Func<Task<byte[]>, DownloadImageResult> DownloadImageContinuationFunction(DownloadImage image)
        {
            return tr =>
            {
                // bad request, server error, or timeout
                if (tr.IsFaulted || tr.IsCanceled)
                    return new DownloadImageResult(image, new byte[0], HttpStatusCode.BadRequest);

                // 404
                if (tr.Result == null || tr.Result.Length == 0)
                    return new DownloadImageResult(image, new byte[0], HttpStatusCode.NotFound);

                return new DownloadImageResult(image, tr.Result, HttpStatusCode.OK);
            };
        }

        private static Func<Task<string>, DownloadHtmlResult> ContinuationFunction(DownloadHtmlDocument html)
        {
            return tr =>
            {
                // bad request, server error, or timeout
                if (tr.IsFaulted || tr.IsCanceled)
                    return new DownloadHtmlResult(html, string.Empty, HttpStatusCode.BadRequest);

                // 404
                if (string.IsNullOrEmpty(tr.Result))
                    return new DownloadHtmlResult(html, string.Empty, HttpStatusCode.NotFound);

                return new DownloadHtmlResult(html, tr.Result, HttpStatusCode.OK);
            };
        }

        private void WaitingForDownloads()
        {
            Receive<DownloadImageResult>(image =>
            {
                HandleImageDownload(image);
                BecomeReady();
            });

            Receive<DownloadHtmlResult>(html =>
            {
                HandleHtmlDownload(html);
                BecomeReady();
            });

            // When we've hit maximum # of concurrent downloads
            Receive<IDownloadDocument>(i => !CanDoDownload, document => Stash.Stash());

            Receive<SetParseActor>(parse =>
            {
                ParseActor = parse.Parser;
            });
        }

        private void BecomeReady()
        {
            //unstash an individual message and become ready again
            Stash.Unstash();
            Become(Ready);
        }

        #region Download handlers


        private void HandleHtmlDownload(DownloadHtmlResult html)
        {
            //take this off the queue of pending downloads
            _currentDownloads.Remove(html.Command);

            //forward the completed HTML download to the CoordinatorActor
            CoordinatorActor.Tell(new CompletedDocument(html.Command.Document, html.Content.Length*2, Self));

            //tell the parser actor to begin processing this document
            ParseActor.Tell(html);
        }

        private void HandleImageDownload(DownloadImageResult image)
        {
            //take this off the queue of pending downloads
            _currentDownloads.Remove(image.Command);

            //forward the completed image download to the CoordinatorActor
            CoordinatorActor.Tell(new CompletedDocument(image.Command.Document, image.Bytes.Length, Self));
        }

        #endregion
    }
}
