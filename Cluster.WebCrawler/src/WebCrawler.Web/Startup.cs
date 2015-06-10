using Microsoft.Owin;
using Owin;
using WebCrawler.Web;

[assembly: OwinStartup(typeof(Startup))]
namespace WebCrawler.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
            ConfigureAuth(app);
        }
    }
}
