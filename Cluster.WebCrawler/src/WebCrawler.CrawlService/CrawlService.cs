using Topshelf;

namespace WebCrawler.CrawlService
{
    class CrawlService : ServiceControl
    {
        public bool Start(HostControl hostControl)
        {
            // start system
            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            // stop system
            return true;
        }
    }
}
