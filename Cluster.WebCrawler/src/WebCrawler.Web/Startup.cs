using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebCrawler.Web.Startup))]
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
