// -----------------------------------------------------------------------
//  <copyright file="ExponentialBackoffTimeout.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

namespace CqrsSqlServer.Backend.Actors;

/// <summary>
/// Utility class for retries / exponential backoff
/// </summary>
public static class ExponentialBackoffTimeout
{
    public static TimeSpan BackoffTimeout(int attemptCount, TimeSpan initialTimeout)
    {
        // going to keep the values small for now
        return initialTimeout + attemptCount * TimeSpan.FromSeconds(1);
    }
}