// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2018 - 2018 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace WebCrawler.TrackerService
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var trackingService = new TrackerService();
            trackingService.Start();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                trackingService.Stop();
                eventArgs.Cancel = true;
            };

            trackingService.WhenTerminated.Wait();
        }
    }
}