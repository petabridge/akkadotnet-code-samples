# Akka.NET PipeTo Sample

The goal of this sample is to show you how you can use the `PipeTo` extension method within [Akka.NET](http://getakka.net/ "Akka.NET - Distributed actor system for C# and F#") to allow a single actor to make many asynchronous calls simultaneously using the [.NET Task Parallel Library (TPL)](https://msdn.microsoft.com/en-us/library/dd460717(v=vs.110).aspx "Task Parallel Library (TPL)").

> This is actually a fairly small and simple example, but we've documented it in extensive detail. Don't confuse "detailed" with "complex."

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

![Akka.NET PipeTo Sample Actor Hierarchy](diagrams/akkadotnet-PipeTo-actor-hierarchy.png)

* **`/user/`** is the root actor for all user-defined actors. Any time you call `ActorSystem.ActorOf` you're going to create a child of the `/user/` actor. This is built into Akka.NET.
* **`/user/consoleReader`** is an instance of a `ConsoleReaderActor` ([source](src/PipeTo.App/Actors/ConsoleActors.cs#L125 "ConsoleReaderActor C# Source")) responsible for prompting the end-user for command-line input. If the user types "exit" on the command line, this actor will also call `ActorSystem.ShutDown` - which will terminate the application. There is only ever a single instance of this actor, because there's only one instance of the command line to read from.
* **`/user/consoleWriter/`** is an instance of a `ConsoleWriterActor` ([source](src/PipeTo.App/Actors/ConsoleActors.cs#L10 "ConsoleWriterActor C# source")) responsible for receiving status updates from all other actors in this sample and writing them to the console in a serial fashion. In the event of a completed feed parse or a failed URL validation, the `ConsoleWriterActor` will tell the `ConsoleReaderActor` to prompt the user for a new RSS / ATOM feed URL. There is only ever a single instance of this actor, because there should only be one actor responsible for writing output to the console (single writer pattern.)
* **`/user/feedValidator`** is an instance of a `FeedValidatorActor` ([source](src/PipeTo.App/Actors/FeedValidatorActor.cs#L13 "FeedValidatorActor C# Source")) responsible for receiving input from the `ConsoleReaderActor` and validating whether or not the user-provided URL is:
	* A valid absolute URI and
	* Actually hosts a valid RSS or Atom feed at the address, determined using [Quick and Dirty Feed Parser](https://github.com/Aaronontheweb/qdfeed "Quick and Dirty Feed Parser - lightweight .NET library for parsing RSS 2.0 and Atom 1.0 XML in an agnostic fashion ")'s asynchronous methods and `PipeTo` ([relevant source](src/PipeTo.App/Actors/FeedValidatorActor.cs#L67 "QDFeed Parser Async call result piped back to FeedValidatorActor").)
* **`/user/feedValidator/[feedCoordinator]`** is an instance of the `FeedParserCoordinator` actor ([source](src/PipeTo.App/Actors/FeedParserCoordinator.cs#L10 "FeedParserCoordinator actor C# Source")) created by the `FeedValidatorActor` in the event that a user-supplied feed URL passes validation. A `FeedParserCoordinator` is responsible for coordinating the downloading of RSS / ATOM feed items, parsing of said items, and the downloading of all images found in the feed concurrently. There is one of these actors per RSS or Atom feed. Technically, if you sent a list of 100 different feed URLs to the `FeedValidatorActor` then it would create 100 different `FeedValidatorActor` instances to process each feed in parallel.  This actor is responsible for dispatching work to its children and determining when its children have finished processing the contents of the provided feed.
* **`/user/feedValidator/[feedCoordinator]/[feedParser]`** hosts an instance of a `FeedParserActor` ([source](src/PipeTo.App/Actors/FeedParserActor.cs#L13 "FeedParserActor C# Source"),) who gets created during the `FeedParserCoordinator.PreStart` call. This actor is responsible for:
	* asynchronously downloading the content of the RSS / ATOM feed using [Quick and Dirty Feed Parser](https://github.com/Aaronontheweb/qdfeed "Quick and Dirty Feed Parser - lightweight .NET library for parsing RSS 2.0 and Atom 1.0 XML in an agnostic fashion ")'s aync methods and using `PipeTo` to deliver the downloaded feed back to itself as a new message ([relevant source](src/PipeTo.App/Actors/FeedParserActor.cs#L73 "FeedParserActor downloads RSS or ATOM feed and uses PipeTo to send result to self").)
	* Parsing all `<img>` tags from each RSS / ATOM item in the feed using the [HTML Agility Pack](http://htmlagilitypack.codeplex.com/ "HTML Agility Pack C# Library").
	* Sending the full URLs for each parsed image to its sibling `HTTPDownloaderActor`, which will begin downloading each image into memory.
	* Reporting back to its parent, the `FeedParserCoordinator` the number of remaining feed items that need to be processed and the number of images that need to be processed.
* **`/user/feedValidator/[feedCoordinator]/[httpDownloader]`** is an instance of a `HttpDownloaderActor` ([source](src/PipeTo.App/Actors/HttpDownloaderActor.cs#L14 "HttpDownloaderActor C# Source")) who gets created during the `FeedParserCoordinator.PreStart` call. This actor is responsible for:
	* asynchronously downloading all image URLs sent to it by the `FeedParserActor` using the `HttpClient` - each download is done asynchronously using `Task` instances, `ContinueWith` for minor post-processing, and `PipeTo` to deliver the completed results back into the `HttpDownloaderActor` as messages. **The fact that this single actor can process many image downloads in parallel is the entire point of this code sample. Please see the ([relevant source](src/PipeTo.App/Actors/HttpDownloaderActor.cs#L98 "Critical region where multiple image downloads are kicked off in parallel").)**. 
	* Reporting successful or failed download attempts back to the `FeedParserCoordinator`.

Also worth pointing out is the use of statically defined names and dynamic names. 

> Any time you intend to have a single instance of a specific actor per-process, you should use a static name so it can be easily referred to via `ActorSelection` throughout your application. 
> 
> If you intend to have many instances of an actor, particularly if they're not top-level actors, then you can use dynamic names.

As a best practice, we define all names and paths for looking up actors inside a static metadata class, in this case the `ActorNames` class ([source](src/PipeTo.App/ActorNames.cs "ActorNames class C# source").)

### Dataflow

The first data flow involves simply reading input from the console, via the `ConsoleReaderActor`:

![ConsoleReaderActor dataflow for reading from console](diagrams/1-dataflow-reading-from-the-console.png)

The `ConsoleReaderActor` receives a message of type `ConsoleReaderActor.ReadFromConsoleClean` from the `Main` method, and this tells the `ConsoleReaderActor` that it's time to print the instructions for the app and request an RSS / ATOM feed Uri from the end-user.

If the `ConsoleReaderActor` receives the string literal "exit" from the end-user, it will call `ActorSystem.ShutDown` and terminate the `Program.MyActorSystem` instance, which will cause `MyActorSystem.AwaitTermination` to complete and allow the `Main` function to exit and terminate the console app.

However, if the `ConsoleReaderActor` receives any other string it will `Tell` that string to the `FeedValidatorActor`.

![FeedValidatorActor dataflow for validating the user-supplied feed uri](diagrams/2-dataflow-validating-feed-uri.png)

The `FeedValidatorActor` receives the `string` message from the `ConsoleReaderActor` and immediately checks to see if the string is a valid Uri.

* **If the string *is not* a valid uri**, the `FeedValidatorActor` sends a `ConsoleWriterActor.ConsoleWriteFailureMessage` back to the `ConsoleWriterActor`.
* **If the string is a valid uri**, the `FeedValidatorActor` will call the following code block to validate that the uri points to a live RSS or ATOM feed.

```csharp
IsValidRssOrAtomFeed(feedUri)
	.ContinueWith(rssValidationResult => new IsValidFeed(feedUri, rssValidationResult.Result), 
                        TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously)
	.PipeTo(Self);
```

This calls a method within [Quick and Dirty Feed Parser](https://github.com/Aaronontheweb/qdfeed "Quick and Dirty Feed Parser - lightweight .NET library for parsing RSS 2.0 and Atom 1.0 XML in an agnostic fashion ") which returns `Task<bool>` - the `FeedValidatorActor` then continues this `Task<bool>` with a simple function that wraps the `bool` result and the original `feedUri` string into a `IsValidFeed` instance. This `IsValidFeed` object is then *piped* into `FeedValidatorActor`'s inbox via the `PipeTo` method.

> If the `FeedValidatorActor` had to validate hundreds of feeds, a single instance of this actor could process hundreds of feeds concurrently because the long-running `IsValidRssOrAtomFeed` method is being run asynchronously on an I/O completion port and the result of that `Task` is placed into the `FeedValidatorActor`'s mailbox just like any other message. 
> 
> That's how you're supposed to use async within an actor - turn all asynchronous operations into functions that eventually produce a new message for the actor to process.

* **If the feed *is not* a valid RSS or ATOM feed**, the `FeedValidatorActor` sends a `ConsoleWriterActor.ConsoleWriteFailureMessage` back to the `ConsoleWriterActor`.
* **If the feed is a valid RSS or ATOM feed**, the `FeedValidatorActor` creates a new `FeedParserCoordinator` actor instance and passes in the `feedUri` as a constructor argument.

![FeedParserCoordinator instantiation dataflow](diagrams/3-dataflow-feed-parser-coordinator-instantiation.png)

When the `FeedParserCoordinator` is created, it immediately creates a `FeedParserActor` and `HttpDownloaderActor` during its `FeedParserCoordinator.PreStart` phase. Once both children are started, the `FeedParserCoordinator` sends a `FeedParserActor.BeginProcessFeed` message to the `FeedParserActor` to begin the feed download and parsing process.

![FeedParserActor feed processing dataflow](diagrams/4-dataflow-feed-parser-actor-feed-processing.png)

Once the `FeedParserActor` receives the `FeedParserActor.BeginProcessFeed` message, it immediately attempts to download and parse the feed using [Quick and Dirty Feed Parser](https://github.com/Aaronontheweb/qdfeed "Quick and Dirty Feed Parser - lightweight .NET library for parsing RSS 2.0 and Atom 1.0 XML in an agnostic fashion ") - and the results are *piped* to the `FeedParserActor` asynchronously as an `IFeed` message. Technically this means that `FeedParserActor` could process multiple feeds in parallel, even though we're really only using the actor to parse a single feed once.

**If the feed is empty or did not parse properly**, the `FeedParserActor` notifies its parent, the `FeedParserCoordinator`, that the job is finished. The `FeedParserCoordinator` will then signal the `ConsoleReaderActor` that the app is ready for additional input and will self-terminate.

**If the feed has items**, the `FeedParserActor` will notify the `FeedParserCoordinator` that there are N RSS / ATOM feed items waiting to be processed and will then begin sending each of those items back to itself as a distinct `ParseFeedItem` message.

![FeedParserActor individual feed item processing dataflow](diagrams/5-dataflow-feed-parser-actor-processing-for-individual-feed-items.png)

For each `ParseFeedItem` message the `FeedParserActor` receives, the actor will:

* Use the [HTML Agility Pack](http://htmlagilitypack.codeplex.com/ "HTML Agility Pack C# Library") to find any `<img>` tags in the text of the feed item and extract the urls of those images.
* Report back to the `FeedParserCoordinator` for each discovered image (to help with job tracking.)
* Send the URL of each image to the `HttpDownloaderActor` for download.
* Tell the `FeedParserCoordinator` that we've completed HTML parsing for one page (job tracking.)

> And so now we get to the important part - seeing the `HttpDownloaderActor` asynchronously download all of the images at once.

![HttpDownloaderActor asynchronous image downloading dataflow](diagrams/6-dataflow-HttpDownloaderActor-asynchronous-image-processing.png)

The `HttpDownloaderActor` receives a `HttpDownloaderActor.DownloadImage` message from the `FeedParserActor`


### NuGet Dependencies

This sample depends on the following [NuGet](http://www.nuget.org/ "NuGet - package manager for.NET") packages in order to run:

* [Akka.NET](http://www.nuget.org/packages/Akka/) (core only)
* [HTML Agility Pack](http://www.nuget.org/packages/HtmlAgilityPack/)
* [Quick and Dirty Feed Parser](http://www.nuget.org/packages/qdfeed/)

## Running the Sample