using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Akka.Configuration;

namespace WebCrawler.Shared.Config
{
    /// <summary>
    /// Used to load <see cref="WebCrawler.Shared.Config"/> objects from stand-alone HOCON files.
    /// </summary>
    public static class HoconLoader
    {
        public static Akka.Configuration.Config ParseConfig(string hoconPath)
        {
            return ConfigurationFactory.ParseString(File.ReadAllText(hoconPath));
        }
    }
}
