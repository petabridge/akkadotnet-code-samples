namespace WebCrawler.Messages.State
{
    /// <summary>
    /// Mergeable, immutable stats class for reporting job progress
    /// </summary>
    public class CrawlJobStats
    {
        public CrawlJobStats(CrawlJob key)
        {
            Key = key;
        }

        public CrawlJob Key { get; private set; }

        public long TotalDocumentsDiscovered => HtmlDocumentsDiscovered + ImagesDiscovered;

        public long HtmlDocumentsDiscovered { get; private set; }

        public long ImagesDiscovered { get; private set; }

        public long TotalDocumentsDownloaded => HtmlDocumentsDownloaded + ImagesDownloaded;

        public long HtmlDocumentsDownloaded { get; private set; }

        public long ImagesDownloaded { get; private set; }

        public long TotalBytesDownloaded => HtmlBytesDownloaded + ImageBytesDownloaded;

        public long HtmlBytesDownloaded { get; private set; }

        public long ImageBytesDownloaded { get; private set; }

        public bool IsEmpty
        {
            get { return TotalDocumentsDiscovered == 0 && TotalBytesDownloaded == 0; }
        }

        /// <summary>
        /// Deep copy funtion
        /// </summary>
        public CrawlJobStats Copy(long? htmlDiscovered = null, long? imgDiscovered = null, long? htmlDownloaded = null,
            long? imgDownloaded = null, long? htmlBytesDownloaded = null, long? imgBytesDownloaded = null)
        {
            return new CrawlJobStats(Key)
            {
                HtmlDocumentsDiscovered = htmlDiscovered ?? HtmlDocumentsDiscovered,
                HtmlDocumentsDownloaded = htmlDownloaded ?? HtmlDocumentsDownloaded,
                ImagesDiscovered = imgDiscovered ?? ImagesDiscovered,
                ImagesDownloaded = imgDownloaded ?? ImagesDownloaded,
                HtmlBytesDownloaded = htmlBytesDownloaded ?? HtmlBytesDownloaded,
                ImageBytesDownloaded = imgBytesDownloaded ?? ImageBytesDownloaded
            };
        }

        /// <summary>
        /// Add the totals from a <see cref="CompletedDocument"/> message to these stats
        /// </summary>
        public CrawlJobStats WithCompleted(CompletedDocument doc)
        {
            if (doc.Document.IsImage)
            {
                return Copy(imgDownloaded: ImagesDownloaded + 1, imgBytesDownloaded: ImageBytesDownloaded + doc.NumBytes);
            }
            return Copy(htmlDownloaded: HtmlDocumentsDownloaded + 1,
                htmlBytesDownloaded: HtmlBytesDownloaded + doc.NumBytes);
        }

        /// <summary>
        /// Add the totals from a <see cref="DiscoveredDocuments"/> message to these stats
        /// </summary>
        public CrawlJobStats WithDiscovered(DiscoveredDocuments doc)
        {
            return Copy(htmlDiscovered: HtmlDocumentsDiscovered + doc.HtmlDocs, imgDiscovered: ImagesDiscovered + doc.Images);
        }

        /// <summary>
        /// Determine if this instance can merge with another <see cref="CrawlJobStats"/>
        /// </summary>
        public bool CanMerge(CrawlJobStats other)
        {
            return (Key.Equals(other.Key));
        }

        /// <summary>
        /// Combine two stats objects
        /// </summary>
        public CrawlJobStats Merge(CrawlJobStats other)
        {
            if (CanMerge(other))
            {
                return Copy(HtmlDocumentsDiscovered + other.HtmlDocumentsDiscovered,
                    ImagesDiscovered + other.ImagesDiscovered,
                    HtmlDocumentsDownloaded + other.HtmlDocumentsDownloaded,
                    ImagesDownloaded + other.ImagesDownloaded,
                    HtmlBytesDownloaded + other.HtmlBytesDownloaded,
                    ImageBytesDownloaded + other.ImageBytesDownloaded);
            }

            return this;
        }

        /// <summary>
        /// Reset the statistics for this <see cref="CrawlJob"/>
        /// </summary>
        public CrawlJobStats Reset()
        {
            return new CrawlJobStats(Key);
        }

        public override string ToString()
        {
            return
                string.Format(
                    "Discovered: {0:N0} (HTML: {1:N0}, IMG: {2:N0}) -- Downloaded {3:N0} (HTML: {4:N0}, IMG: {5:N0}) -- Bytes {6:N0} (HTML: {7:N0}, IMG: {8:N0})",
                    TotalDocumentsDiscovered,
                    HtmlDocumentsDiscovered, ImagesDiscovered, TotalDocumentsDownloaded, HtmlDocumentsDownloaded,
                    ImagesDownloaded, TotalBytesDownloaded, HtmlBytesDownloaded, ImageBytesDownloaded);
        }
    }
}
