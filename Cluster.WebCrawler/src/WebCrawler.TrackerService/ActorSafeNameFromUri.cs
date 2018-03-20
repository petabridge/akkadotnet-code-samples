// -----------------------------------------------------------------------
// <copyright file="ActorSafeNameFromUri.cs" company="Petabridge, LLC">
//      Copyright (C) 2018 - 2018 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace WebCrawler.TrackerService
{
    /// <summary>
    ///     Convert a URI into an actor-friendly name
    /// </summary>
    public static class ActorSafeNameFromUri
    {
        public static string ToActorName(this Uri uri)
        {
            return Uri.EscapeDataString(uri.ToString());
        }
    }
}