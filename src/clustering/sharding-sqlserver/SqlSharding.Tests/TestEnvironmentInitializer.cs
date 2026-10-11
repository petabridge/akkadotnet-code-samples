using System.Runtime.CompilerServices;

namespace SqlSharding.Tests;

internal static class TestEnvironmentInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Disable config file watching in test hosts.
        // Prevents file descriptor exhaustion (inotify watch limit) on Linux
        // when running many Akka.Hosting.TestKit tests.
        Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");
    }
}
