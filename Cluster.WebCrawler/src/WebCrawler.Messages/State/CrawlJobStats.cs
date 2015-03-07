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

        public int TotalDocumentsDiscovered { get { return HtmlDocumentsDiscovered + ImagesDiscovered; } }

        public int HtmlDocumentsDiscovered { get; private set; }

        public int ImagesDiscovered { get; private set; }

        public int TotalDocumentsDownloaded { get { return HtmlDocumentsDownloaded + ImagesDownloaded; } }

        public int HtmlDocumentsDownloaded { get; private set; }

        public int ImagesDownloaded { get; private set; }

        public int TotalBytesDownloaded { get { return HtmlBytesDownloaded + ImageBytesDownloaded; } }

        public int HtmlBytesDownloaded { get; private set; }

        public int ImageBytesDownloaded { get; private set; }

        public bool IsEmpty
        {
            get { return TotalDocumentsDiscovered == 0; }
        }

        /// <summary>
        /// Deep copy funtion
        /// </summary>
        public CrawlJobStats Copy(int? htmlDiscovered = null, int? imgDiscovered = null, int? htmlDownloaded = null,
            int? imgDownloaded = null, int? htmlBytesDownloaded = null, int? imgBytesDownloaded = null)
        {
            return new CrawlJobStats(Key)
            {
                HtmlDocumentsDiscovered = htmlDiscovered ?? HtmlDocumentsDiscovered,
                HtmlDocumentsDownloaded = htmlDownloaded ?? HtmlDocumentsDownloaded,
                ImagesDiscovered = imgDiscovered ?? ImagesDiscovered,
                ImagesDownloaded = imgDownloaded ?? ImagesDownloaded,
                HtmlBytesDownloaded = htmlBytesDownloaded ?? TotalBytesDownloaded,
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
                    "Discovered: {0} (HTML: {1}, IMG: {2}) -- Downloaded {3} (HTML: {4}, IMG: {5}) -- Bytes {6} (HTML: {7}, IMG: {8})",
                    TotalDocumentsDiscovered,
                    HtmlDocumentsDiscovered, ImagesDiscovered, TotalDocumentsDownloaded, HtmlDocumentsDownloaded,
                    ImagesDownloaded, TotalBytesDownloaded, HtmlBytesDownloaded, ImageBytesDownloaded);
        }
    }
}
