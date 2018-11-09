using System;

namespace WebCrawler.CrawlService
{
    class Program
    {
        static void Main(string[] args)
        {
            var crawlerService = new CrawlerService();
            crawlerService.Start();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                crawlerService.Stop();
                eventArgs.Cancel = true;
            };

            crawlerService.WhenTerminated.Wait();
        }
    }
}
