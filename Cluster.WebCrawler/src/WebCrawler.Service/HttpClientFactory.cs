using System.Net.Http;

namespace WebCrawler.Service
{
    /// <summary>
    /// Factory class for creating <see cref="HttpClient"/> instances
    /// </summary>
    public static class HttpClientFactory
    {
        public static HttpClient GetClient()
        {
            return new HttpClient();
        }
    }
}
