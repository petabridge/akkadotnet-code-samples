using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Akka.Actor;
using Akka.Util;
using WebCrawler.Messages.State;
using WebCrawler.Service.State;

namespace WebCrawler.Service.Actors.IO
{
    /// <summary>
    /// Actor responsible for using <see cref="HttpClient"/>
    /// </summary>
    public class DownloadWorker : ReceiveActor, IWithUnboundedStash
    {
        #region Messages

        public interface IDownloadDocument
        {
            CrawlDocument Document { get; }
        }

        public class DownloadHtmlDocument : IDownloadDocument, IEquatable<DownloadHtmlDocument>
        {
            public DownloadHtmlDocument(CrawlDocument document)
            {
                Document = document;
            }

            public CrawlDocument Document { get; private set; }

            public bool Equals(DownloadHtmlDocument other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(Document, other.Document);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((DownloadHtmlDocument)obj);
            }

            public override int GetHashCode()
            {
                return (Document != null ? Document.GetHashCode() : 0);
            }
        }

        public class DownloadImage : IDownloadDocument, IEquatable<DownloadImage>
        {
            public DownloadImage(CrawlDocument document)
            {
                Document = document;
            }

            public CrawlDocument Document { get; private set; }

            public bool Equals(DownloadImage other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(Document, other.Document);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((DownloadImage)obj);
            }

            public override int GetHashCode()
            {
                return (Document != null ? Document.GetHashCode() : 0);
            }
        }

        /// <summary>
        /// Results from a <see cref="DownloadImage"/> operation
        /// </summary>
        public class DownloadImageResult
        {
            public DownloadImageResult(DownloadImage command, byte[] bytes, HttpStatusCode status)
            {
                Status = status;
                Bytes = bytes;
                Command = command;
            }

            public DownloadImage Command { get; private set; }

            public byte[] Bytes { get; private set; }

            public HttpStatusCode Status { get; private set; }
        }

        /// <summary>
        /// Results form a <see cref="DownloadHtmlDocument"/> operation
        /// </summary>
        public class DownloadHtmlResult
        {
            public DownloadHtmlResult(DownloadHtmlDocument command, string content, HttpStatusCode status)
            {
                Status = status;
                Content = content;
                Command = command;
            }

            public DownloadHtmlDocument Command { get; private set; }

            public string Content { get; private set; }

            public HttpStatusCode Status { get; private set; }
        }

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
                _httpClient.GetStringAsync(html.Document.DocumentUri).ContinueWith(tr =>
                {
                    // bad request, server error, or timeout
                    if (tr.IsFaulted || tr.IsCanceled)
                        return new DownloadHtmlResult(html, string.Empty, HttpStatusCode.BadRequest);

                    // 404
                    if (string.IsNullOrEmpty(tr.Result))
                        return new DownloadHtmlResult(html, string.Empty, HttpStatusCode.NotFound);

                    return new DownloadHtmlResult(html, tr.Result, HttpStatusCode.OK);
                }).PipeTo(Self);
            });

            Receive<DownloadImage>(i => CanDoDownload, image =>
            {
                _currentDownloads.Add(image);
                _httpClient.GetByteArrayAsync(image.Document.DocumentUri).ContinueWith(tr =>
                {
                    // bad request, server error, or timeout
                    if (tr.IsFaulted || tr.IsCanceled)
                        return new DownloadImageResult(image, new byte[0], HttpStatusCode.BadRequest);

                    // 404
                    if (tr.Result == null || tr.Result.Length == 0)
                        return new DownloadImageResult(image, new byte[0], HttpStatusCode.NotFound);

                    return new DownloadImageResult(image, tr.Result, HttpStatusCode.OK);
                }).PipeTo(Self);
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
