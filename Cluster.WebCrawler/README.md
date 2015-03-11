# Akka.NET Clustering Sample: WebCrawler

The goal of this sample is to show you how to use `Akka.Cluster` to form resilient systems that can scale out across multiple processes or machines without complicated user-defined code or expensive tools.

In this sample you'll also see how to integrate Akka.NET with the following technologies:

- **[ASP.NET MVC5](http://www.asp.net/mvc/mvc5)**;
- **[SignalR](http://signalr.net/ "Websockets for .NET")** - Websockets library for .NET;
-  **[Topshelf](http://topshelf-project.com/ "Topshelf Project - easily turn console apps into Windows Services")** - Windows Services made easy; and
- **[Lighthouse](https://github.com/petabridge/lighthouse "Lighthouse - Service Discovery for Akka.NET")** - a lightweight service discovery platform for Akka.Cluster.

## Sample Overview

In the `WebCrawler` sample we're actually going to run three different pieces of software concurrently:

* **`[Lighthouse]`** - An instance of the **[Lighthouse](https://github.com/petabridge/lighthouse "Lighthouse - Service Discovery for Akka.NET")** service, so you'll want to clone and build that repository in you intend to run this sample;
* **`[Crawler]`** - A dedicated Windows Service built using **[Topshelf](http://topshelf-project.com/ "Topshelf Project - easily turn console apps into Windows Services")**. This is where most of the heavy lifting is done in this sample, and multiple instances of these services can be run in parallel in order to cooperatively execute a web crawl. The source for `[Crawler]` is inside the [`src\WebCrawler.Service`](src\WebCrawler.Service) folder.
* **`[Web]`** - An ASP.NET MVC5 application that uses **[SignalR](http://signalr.net/ "Websockets for .NET")** to send commands to and receive data from `[Crawler]` instances. Multiple `[Web]` instances may be run in parallel, but they're unaware of eachother's crawl jobs by default.

> NOTE: `WebCrawler.sln` should attempt to launch one instance of `[Crawler]` and `[Web]` by default.

### Goal: Use Microservices with Akka.Cluster to Crawl Specific Domains

The goal of this sample is to have all three of these services work cooperatively together to crawl multiple web domains in parallel. Here's what the data flow of an individual domain crawl looks like:







 