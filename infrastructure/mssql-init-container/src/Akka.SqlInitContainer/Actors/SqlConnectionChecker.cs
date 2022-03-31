using System.Data;
using System.Data.Common;
using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.DependencyInjection;

namespace Akka.SqlInitContainer.Actors;

/// <summary>
/// Used to ping the database until a connection is available
/// </summary>
public sealed class SqlConnectionChecker : ReceiveActor, IWithTimers
{
    private readonly IServiceProvider _provider;
    private readonly ILoggingAdapter _logging = Context.GetLogger();
    private const string CheckConnectionKey = "checkConnection";

    public SqlConnectionChecker(IServiceProvider provider)
    {
        _provider = provider;
        
        ReceiveAsync<CheckSql>(async sql =>
        {
            try
            {
                // create a new scope for managing database connections
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var scope = _provider.CreateScope();

                var sqlConnection = scope.ServiceProvider.GetService<DbConnection>();
                await sqlConnection.OpenAsync(cts.Token);
            }
            catch (Exception ex)
            {
                _logging.Warning(ex, "Unable to connect to SqlServer. Retrying in 5s...");
                ScheduleCheck();
                return;
            }
            
            // if we made it this far and the connection string is correct - Akka database
            // is created and the database is ready for use. Shut the ActorSystem down and
            // terminate the InitContainer so the Akka.NET application can run.
#pragma warning disable CS4014
            Context.System.Terminate();
#pragma warning restore CS4014
        });
    }

    private void ScheduleCheck()
    {
        Timers!.StartSingleTimer(CheckConnectionKey, CheckSql.Instance, TimeSpan.FromSeconds(5));
    }

    protected override void PreStart()
    {
        // schedule immediate check for SQL
        Self.Tell(CheckSql.Instance);
    }

    private sealed class CheckSql
    {
        public static readonly CheckSql Instance = new();
        private CheckSql(){}
    }

    public ITimerScheduler? Timers { get; set; }
}