# Akka.NET PipeTo Sample

The goal of this sample is to show you how you can use the `PipeTo` extension method within [Akka.NET](http://getakka.net/ "Akka.NET - Distributed actor system for C# and F#") to allow a single actor to make many asynchronous calls simultaneously using the [.NET Task Parallel Library (TPL)](https://msdn.microsoft.com/en-us/library/dd460717(v=vs.110).aspx "Task Parallel Library (TPL)").

## Sample Overview

In this sample we're going to ask the user to provide us with the URL of a valid RSS or ATOM feed, and we're going to:

* Validate that the URL resolves to an actual RSS / ATOM feed;
* Parse all of the `<img>` tags out of the bodies of the items in the feed; and
* Asynchronously download all images for each blog post in parallel *using a single actor*, even though [Akka.NET actors can only process one message at a time](http://petabridge.com/blog/akkadotnet-what-is-an-actor/ "What is an Akka.NET actor?")!

The goal of this is to show you that, yes - even though actors can only process one message at a time they can still leverage `async` methods and `Task<T>` objects to do multiple things in parallel.

> **Note**: with some fairly small changes, you could modify this sample to create an offline, local backup of every blog post in a remote RSS or ATOM feed. 
> 
> This code can also process multiple RSS or ATOM feeds in parallel without any modification - you'd just need to change the user interface to be able to provide multiple feed URLs at once. 
> 
> Maybe you should give one of these suggestions a try once you've had a chance to grok the sample? ;)

### Architecture

Everything in this sample, including reading from and writing to the `Console`, is done using Akka.NET actors. 

Here's how the **actor hierarchy** is organized in this sample:

![Akka.NET PipeTo Sample Actor Hierarchy](/diagrams/akkadotnet-PipeTo-actor-hierarchy.png)

* **`/user/`** is the root actor for all user-defined actors. Any time you call `ActorSystem.ActorOf` you're going to create a child of the `/user/` actor. This is built into Akka.NET.
* **`/user/consoleReader`** is an instance of a `ConsoleReaderActor` (source) responsible for prompting the end-user for command-line input. If the user types "exit" on the command line, this actor will also call `ActorSystem.ShutDown` - which will terminate the application. There is only ever a single instance of this actor, because there's only one instance of the command line to read from.
* **`/user/consoleWriter/`** is an instance of a `ConsoleWriterActor` (source) responsible for receiving status updates from all other actors in this sample and writing them to the console in a serial fashion. In the event of a completed feed parse or a failed URL validation, the `ConsoleWriterActor` will tell the `ConsoleReaderActor` to prompt the user for a new RSS / ATOM feed URL. There is only ever a single instance of this actor, because there should only be one actor responsible for writing output to the console (single writer pattern.)

### NuGet Dependencies

This sample depends on the following [NuGet](http://www.nuget.org/ "NuGet - package manager for.NET") packages in order to run:

* [Akka.NET](http://www.nuget.org/packages/Akka/) (core only)
* [HTML Agility Pack](http://www.nuget.org/packages/HtmlAgilityPack/)
* [Quick and Dirty Feed Parser](http://www.nuget.org/packages/qdfeed/)

This sample is a simple .NET 4.5 console application that does the following:

1. `ConsoleReaderActor` asks the user to type the URL of a valid RSS or ATOM feed into the console, such as [http://www.aaronstannard.com/feed.xml](http://www.aaronstannard.com/feed.xml).
2. URL is sent to the `FeedValidatorActor` who then validates the URL and asynchronously determines whether or not the destination content is valid RSS / ATOM, using [Quick and Dirty Feed Parser](https://github.com/Aaronontheweb/qdfeed "Quick and Dirty Feed Parser - lightweight .NET library for parsing RSS 2.0 and Atom 1.0 XML in an agnostic fashion ")'s asynchronous methods. If the URL doesn't meet either of these two validation requirements, the user is prompted to start the process over by providing a different URL. Otherwise, the application moves onto step 3.
3. The `FeedValidatorActor` creates a `FeedParserCoordinator` actor

## Critical Sections

## Running the Sample