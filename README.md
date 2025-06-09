# Akka.NET Code Samples

[Akka.NET](https://getakka.net/ "Akka.NET - .NET distributed actor framework") is a radically different way of developing concurrent and networked applications in .NET, and so it's important to have a number of high quality reference applications developers can explore in order to better understand how to design and model software using Actors and Akka.NET.

The goals of this repository are to provide users with shovel-ready Akka.NET samples that follow "[Pit of Success](https://blog.codinghorror.com/falling-into-the-pit-of-success/)" paradigms for the following areas:

* Akka.NET features: Persistence, Clustering, and Streams;
* Integrating Akka.NET with other popular technologies: RabbitMQ, Apache Kafka, and more; and
* Deploying Akka.NET into popular and common deployment environments: Kubernetes, Azure, AWS, and more.

These samples aren't designed to teach how to model complex domains using actors - they are primarily designed to demonstrate copy-and-pasteable approaches to running AKka.NET infrastructure correctly and succinctly.

## Current Samples:

1. [Akka.NET Cluster.Sharding with Akka.Persistence.SqlServer and Razor Pages](https://github.com/petabridge/akkadotnet-code-samples/tree/master/src/clustering/sharding-sqlserver)
2. [Akka.Streams.Amqp.RabbitMQ with Akka.Cluster.Sharding - Reliable Delivery + Backpressure Support](https://github.com/petabridge/akkadotnet-code-samples/tree/master/src/reliability/rabbitmq-backpressure)
3. [Event-Sourcing and CQRS with Akka.Persistence and Akka.Persistence.Query](https://github.com/petabridge/akkadotnet-code-samples/tree/master/src/cqrs/cqrs-sqlserver)
4. [Autofac And Akka.Hosting Integration](https://github.com/petabridge/akkadotnet-code-samples/tree/master/src/dependency-injection/AutofacIntegration)
5. [Akka.NET Cluster Setup Using Microsoft .NET Aspire](https://github.com/petabridge/akkadotnet-code-samples/tree/master/src/clustering/cluster-bootstrap/Aspire)

## Contributing

We accept pull requests for new samples or changes to existing ones, but we maintain a strict quality standard;

1. All samples should be framed as self-hosted services and should rely on https://github.com/akkadotnet/Akka.Hosting for management of the `ActorSystem`;
2. Samples that require multiple nodes to operate should, ideally, be run inside of Kubernetes or `docker compose` ;
3. Each sample needs a complete `README.md` that explains what the sample does, what a user needs to run it, and a what types of steps they need to execute it;
4. If a sample requires Kubernetes or `docker compose` then a `.cmd` and `.sh` script must be provided to setup the infrastructure and another to tear it down;
5. Samples should not ship with a full blown `nuke` build system - that's overkill;
6. All samples should reference a single `Directory.Build.props` file (it's already in this repository, don't add a new one) which will determine the version of Akka.NET, Akka.Hosting, ASP.NET, .NET Runtime, and Microsoft.Extensions.* used;
7. The samples should be contained in their own solution files, rather than one giant one; and
8. Samples should be coherently organized by folder.

### Questions?

Please [create a Github issue](https://github.com/petabridge/akkadotnet-code-samples/issues) for any questions you might have.

### Code License

All source code is licensed under the language of Apache 2.0. See [LICENSE](LICENSE) for more details.

### Diagrams and Visuals

All of the visuals used to explain our samples are licensed under [Creative Commons Attribution 4.0 International](http://creativecommons.org/licenses/by/4.0/).

![Creative Commons Attribution 4.0 International License](images/creative-commons.png)

You are free to modify and use these diagrams in your own derivative works as long as you comply with the text of the [Creative Commons Attribution 4.0 International](http://creativecommons.org/licenses/by/4.0/) license.

## About Petabridge

![Petabridge logo](images/petabridge_logo.png)

[Petabridge](https://petabridge.com/) is a company dedicated to making it easier for .NET developers to build distributed applications.

Petabridge provides [Akka.NET support, consulting, and training](https://petabridge.com/services/support/).

---
Copyright 2015 - 2024 Petabridge, LLC
