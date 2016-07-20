using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Actors;
using Akka.Streams.Dsl;
using WebCrawler.Shared.IO.Messages;
using WebCrawler.TrackingService.State;

namespace WebCrawler.Shared.IO
{
    public static class DownloadFlow
    {
        public static Flow<CrawlDocument, IDownloadDocument, NotUsed> SelectDocType()
        {
            return Flow.Create<CrawlDocument>()
                .Select(x => (x.IsImage ? new DownloadImage(x) : (IDownloadDocument)new DownloadHtmlDocument(x)));
        }

        public static Flow<IDownloadDocument, DownloadHtmlResult, NotUsed> ProcessHtmlDownloadFor(int degreeOfParallelism, HttpClient client)
        {
            return Flow.Create<IDownloadDocument>()
                .Where(x => x is DownloadHtmlDocument)
                .Select(x => (DownloadHtmlDocument) x)
                .SelectAsyncUnordered(degreeOfParallelism,
                    document =>
                        client.GetStringAsync(document.Document.DocumentUri)
                            .ContinueWith(HtmlContinuationFunction(document)));
        }

        private static Func<Task<string>, DownloadHtmlResult> HtmlContinuationFunction(DownloadHtmlDocument html)
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

        public static Flow<IDownloadDocument, DownloadImageResult, NotUsed> ProcessImageDownloadFor(
            int degreeOfParallelism, HttpClient client)
        {
            return Flow.Create<IDownloadDocument>()
                .Where(x => x is DownloadImage)
                .Select(x => (DownloadImage)x)
                .SelectAsyncUnordered(degreeOfParallelism,
                    document =>
                        client.GetByteArrayAsync(document.Document.DocumentUri)
                            .ContinueWith(DownloadImageContinuationFunction(document)));
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
    }
}
