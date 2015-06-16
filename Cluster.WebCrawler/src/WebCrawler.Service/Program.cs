using Topshelf;

namespace WebCrawler.TrackingService
{
    class Program
    {
        static int Main(string[] args)
        {
            return (int)HostFactory.Run(x =>
            {
                x.SetServiceName("Tracker");
                x.SetDisplayName("Akka.NET Crawl Tracker");
                x.SetDescription("Akka.NET Cluster Demo - Web Crawler.");

                x.UseAssemblyInfoForServiceInfo();
                x.RunAsLocalSystem();
                x.StartAutomatically();
                //x.UseNLog();
                x.Service<TrackerService>();
                x.EnableServiceRecovery(r => r.RestartService(1));
            });
        }
    }
}
