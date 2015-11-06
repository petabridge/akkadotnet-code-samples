# Akka.NET Professional Code Samples

![Akka.NET logo](images/akka_net_logo.png)

[Akka.NET](http://getakka.net/ "Akka.NET - .NET distributed actor framework") is a radically different way of developing concurrent and networked applications in .NET, and so it's important to have a number of high quality reference applications developers can explore in order to better understand how to design and model software using Actors and Akka.NET.

That's the goal of this repository - to provide a small number of *exceptionally* well-explained and documented examples .NET architects and developers can use to better understand how to take advantage of Akka.NET's full potential.

## Current Samples

**[Doing Asynchronous Operations inside Actors with PipeTo](/PipeTo/)** - how to use `Task<T>` and `async` operations inside your actors using the `PipeTo` pattern in Akka.NET.

**[Testing actors with`Akka.TestKit`](/TestKit/)** — how to test your `ActorSystem`s, explanation of the core & advanced features of the testing framework, as well as addressing common testing FAQs and situations.

**[ASP.NET and Windows Service Microservices with Akka.Cluster](Cluster.WebCrawler)** - build an elastically scalable web-crawler using Akka.Cluster in Windows Services and ASP.NET MVC.

**[Remote Deployment of Actors with Akka.Remote](RemoteDeploy/)** - how to deploy actors over the network using the Akka.Remote module.

## Contributing

We accept pull requests for new samples or changes to existing ones, but we maintain a very high standard of quality.

Please see our [PipeTo Sample](/PipeTo/) for an example.

Any samples you want to submit must:

* Contain detailed comments in the source code;
* Have a detailed `README` explaining your architectural choices and data flows;
* Be markedly distinct from any other sample in this repository;
* Be concise enough that a single developer can review it easily.

### Questions about Samples?

Please [create a Github issue](https://github.com/petabridge/akkadotnet-code-samples/issues) for any questions you might have.

### Code License

All source code is licensed under the language of Apache 2.0. See [LICENSE](LICENSE) for more details.

### Diagrams and Visuals

All of the visuals used to explain our samples are licensed under [Creative Commons Attribution 4.0 International](http://creativecommons.org/licenses/by/4.0/).

![Creative Commons Attribution 4.0 International License](images/creative-commons.png)

You are free to modify and use these diagrams in your own derivative works as long as you comply with the text of the [Creative Commons Attribution 4.0 International](http://creativecommons.org/licenses/by/4.0/) license.

All original diagram files are in `.sdr` format, which means they were made with [SmartDraw](http://www.smartdraw.com/ "SmartDraw - communicate visually with great diagrams for Windows").

![SmartDraw logo](images/smartdraw-logo.jpg)

You can [download a free trial of SmartDraw from their site](http://www.smartdraw.com/downloads/).

## About Petabridge

![Petabridge logo](images/petabridge_logo.png)

[Petabridge](http://petabridge.com/) is a company dedicated to making it easier for .NET developers to build distributed applications.

Petabridge provides Akka.NET consulting and training, including advanced training in [Akka.Remote](https://petabridge.com/training/akka-remoting/), [Akka.Cluster](https://petabridge.com/training/akka-clustering/), and [Akka.NET Design Patterns](https://petabridge.com/training/akka-design-patterns/)!

---
Copyright 2015 Petabridge, LLC

