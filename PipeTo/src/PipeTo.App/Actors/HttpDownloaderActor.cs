using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Akka.Actor;

namespace PipeTo.App.Actors
{
    /// <summary>
    /// Downloads content over HTTP asynchronously using <see cref="PipeTo"/>
    /// </summary>
    public class HttpDownloaderActor : ReceiveActor
    {
        #region Message types

        /// <summary>
        /// Command used to initiate the download of an image
        /// </summary>
        public class DownloadImage
        {
            public DownloadImage(string feedUri, string imageUrl)
            {
                ImageUrl = imageUrl;
                FeedUri = feedUri;
            }

            public string FeedUri { get; private set; }

            public string ImageUrl { get; private set; }
        }

        /// <summary>
        /// Result of an asynchronous task from the HttpClient
        /// </summary>
        public class ImageDownloadResult
        {
            public ImageDownloadResult(DownloadImage imageDownloadCommand, HttpStatusCode statusCode) : this(imageDownloadCommand, statusCode, null)
            {
            }

            public ImageDownloadResult(DownloadImage imageDownloadCommand, HttpStatusCode statusCode, Stream content)
            {
                Content = content;
                StatusCode = statusCode;
                ImageDownloadCommand = imageDownloadCommand;
            }
       

            public DownloadImage ImageDownloadCommand { get; private set; }

            public HttpStatusCode StatusCode { get; private set; }

            /// <summary>
            /// Can be null!
            /// </summary>
            public Stream Content { get; private set; }
        }

        #endregion

        private readonly HttpClient _httpClient;
        private readonly string _consoleWriterActorPath;

        public HttpDownloaderActor() : this(ActorNames.ConsoleWriterActor.Path) { }

        public HttpDownloaderActor(string consoleWriterPath) : this(new HttpClient(), consoleWriterPath) { }

        public HttpDownloaderActor(HttpClient httpClient, string consoleWriterActorPath)
        {
            _httpClient = httpClient;
            _consoleWriterActorPath = consoleWriterActorPath;
            Initialize();
        }

        /// <summary>
        /// Used to define all of our <see cref="Receive"/> hooks for <see cref="HttpDownloaderActor"/>
        /// </summary>
        private void Initialize()
        {
            //Command to begin downloading an image
            Receive<DownloadImage>(image => 
            {
                 SendMessage(string.Format("Beginning download of img {0} for feed {1}", image.ImageUrl, image.FeedUri));

                //check for relative URLs
                var imageUrl = image.ImageUrl;
                if (!Uri.IsWellFormedUriString(image.ImageUrl, UriKind.Absolute))
                {
                    var baseAddress = new Uri(image.FeedUri);

                    //Combine the base address and relative URL of image to form an absolute one.
                    imageUrl = string.Format("{0}://{1}{2}", baseAddress.Scheme, baseAddress.Host, imageUrl);
                }

                //asynchronously download the image and pipe the results to ourself
                _httpClient.GetAsync(imageUrl).ContinueWith(httpRequest =>
                {
                    var response = httpRequest.Result;

                    //successful img download - which happened in a DIFFERENT THREAD
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        // async call, inside an async call
                        // and we wait on it...
                        // BUT THIS IS STILL ASYNCHRONOUS?!?!
                        // INSERT INCEPTION HORN SOUND EFFECT HERE https://www.youtube.com/watch?v=ZKGJZt83_JE
                        var contentStream = response.Content.ReadAsStreamAsync();
                        try
                        {
                            contentStream.Wait(TimeSpan.FromSeconds(1));
                            return new ImageDownloadResult(image, response.StatusCode, contentStream.Result);
                        }
                        catch //timeout exceptions!
                        {
                            return new ImageDownloadResult(image, HttpStatusCode.PartialContent);
                        }
                    }

                    return new ImageDownloadResult(image, response.StatusCode);
                }, TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously).PipeTo(Self);
            });

            //Process the results of our asynchronous download
            Receive<ImageDownloadResult>(imagedownload =>
            {
                //Successful download
                if (imagedownload.StatusCode == HttpStatusCode.OK)
                {
                    //Print a status message
                    SendMessage(string.Format("Successfully downloaded image {0} [{1}kb]",
                        imagedownload.ImageDownloadCommand.ImageUrl, imagedownload.Content.Length/1000), PipeToSampleStatusCode.Success);
                   
                }
                else //failed download
                {
                    //Print a status message
                    SendMessage(string.Format("Failed to download image {0}",
                        imagedownload.ImageDownloadCommand.ImageUrl));
                }

                //Let the coordinator know that we've made progress, even if the download failed
                Context.Parent.Tell(
                    new FeedParserCoordinator.DownloadComplete(imagedownload.ImageDownloadCommand.FeedUri, 0, 1));
            });
        }

        #region Messaging methods

        private void SendMessage(string message, PipeToSampleStatusCode pipeToSampleStatus = PipeToSampleStatusCode.Normal)
        {
            //create the message instance
            var consoleMsg = StatusMessageHelper.CreateMessage(message, pipeToSampleStatus);

            //Select the ConsoleWriterActor and send it a message
            Context.ActorSelection(_consoleWriterActorPath).Tell(consoleMsg);
        }

        #endregion

    }
}
