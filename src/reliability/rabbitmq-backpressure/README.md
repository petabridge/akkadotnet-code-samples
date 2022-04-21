# Akka.Streams.Amqp.RabbitMQ with Akka.Cluster.Sharding - Reliable Delivery + Backpressure Support

The goal of this sample is to demonstrate how to use [Akka.Streams](https://getakka.net/articles/streams/introduction.html), and more specifically the [Alpakka connectors for RabbitMQ](https://github.com/akkadotnet/Alpakka), to build a reliable delivery mechanism that sends messages off to a set of actors distributed in an Akka.NET cluster via [Akka.Cluster.Sharding](https://getakka.net/articles/clustering/cluster-sharding.html).

## Technology

This solution is built with:

- .NET 6 minimal APIs;
- C# `record` types;
- Akka.NET v1.4 w/ Akka.Cluster;
- [Akka.Streams.Amqp.RabbitMq](https://www.nuget.org/packages/Akka.Streams.Amqp.RabbitMq); and
- [Akka.Hosting](https://github.com/akkadotnet/Akka.Hosting) - which minimizes the amount of configuration for Akka.NET to practically zero.

## Domain

We have two applications that can be run concurrently or independently in this sample:

1. `ReliableRabbitMQ.Producer` - a stand-alone console process which pumps messages into a RabbitMQ instance and
2. `ReliableRabbitMQ.Consumer` - a clustered Akka.NET process that uses a `ClusterSingletonManager` to run a single consumer from the RabbitMQ queue that `ReliableRabbitMQ.Producer` writes to and subsequently delivers its messages over the network via an Akka.Cluster.Sharding `ShardRegion` to a variety of `ProductActor`s, each of which has a chance of successfully processing the message or simulating a network failure and allowing it to timeout.

The domain is very simple as this sample is designed to teach infrastructure patterns primarily.

## Backpressure

However, the most important and nuanced detail of this sample is the notion of _backpressure_ the ability to slow down the rate at which messages are pulled from RabbitMQ in the event that the cluster becomes unavailable or too backlogged to process messages promptly.

> "Backpressure," in layman's terms, is what happens in a asynchronous producer-consumer relationship when the producer produces at a faster rate than the consumer conumes. The "backpressure" eventually overwhelms the consumer and causes it to consistently fail as it simply can't keep up with the amount of work that is being sent to it. This is a fairly common problem in many asynchronous systems.

Backpressure can be mitigated via a variety of strategies (and this is Akka.Streams's speciality) - batching, buffering, aggregating, and dropping messages are all examples. However, the _best_ approach when you need to guarantee processing for every message is to simply pause message production from the consumer to the producer - and this works very well in concert with a durable queue such as RabbitMQ. 

The messages simply stay put inside RabbitMQ until the downstream Akka.NET consumers signal that they are ready for production once again. This allows the slower consumers to dictate the rate at which work is executed, resolving backpressure issues when the system is too busy to process everything that is produced all at once.

## Failure Handling

Backpressure is one concern, but it goes hand-in-hand with reliability. How do we guarantee that every message is processed at least once inside this system?

![RabbitMQ Akka.NET Streams Consumer + Retry](images/rabbitMQ-backpressure.png)

Our Akka.Streams graph on the consumer side is the key to making sure 