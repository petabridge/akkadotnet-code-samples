# Akka.NET Cluster.Sharding with Akka.Persistence.SqlServer and Razor Pages

The goal of this sample is to demonstrate how to host Akka.Cluster.Sharding with persistent entity actors backed by Microsoft SQL Server.

## Technology

This solution is built with:

- Minimal APIs;
- C# `record` types;
- ASP.NET Core;
- Google.Protobuf for message and state schema;
- Akka.NET v1.5 w/ Akka.Cluster;
- Akka.Persistence.Sql; and
- [Akka.Hosting](https://github.com/akkadotnet/Akka.Hosting) - which minimizes the amount of configuration for Akka.NET to practically zero.


## Domain

Like all of the samples in this repository, we are using a simple domain since the focus of the sample is meant to be _how to use Akka.NET infrastructure succinctly and successfully_. That being said, it's still worth understanding what our domain does: product inventory + revenue tracking.

This app consists of two Akka.Cluster role types:

1. `SqlSharding.WebApp` - a web-application that joins the cluster and queries actors hosted on the `SqlSharding.Sql.Host` instances via Akka.Cluster.Sharding and Akka.Cluster.Singleton  and
2. `SqlSharding.Sql.Host` - a [headless Akka.NET process](https://petabridge.com/blog/akkadotnet-ihostedservice/) that hosts the `ShardRegion` for all `ProductTotalsActor` instances and a `ClusterSingletonManager` for the singular `ProductIndexActor` instance.

We have only two actor types in this solution:

1. `ProductTotalsActor` - our `ReceivePersistentActor` that will process `IProductCommand `, transforming them into (0..N) `IProductEvent`s, updating its `ProductState`, and replying to the original sender with a `ProductCommandResponse` with the results of each command. This entity actor demonstrates one of the more robust ways of executing event-sourcing on top of Akka.Persistence using C#9 `record` types and cleanly separating command / event / query message types from each other.
2. `ProductIndexActor` - uses [Akka.Persistence.Query](https://getakka.net/articles/persistence/persistence-query.html) to receive the list of all available products, queries the `ProductTotalsActor` for some display data, but otherwise just serves up copies of its index to the `/` HTTP route each time it's asked.

> **N.B.** while this application uses event-sourcing heavily, it does not use CQRS. One CQRS modification you would likely find in a production system is that the `ProductIndexActor` would very likely be changed to insert all of the relevant product index data into rows in a SQL table as a materialized view, rather than handle queries directly from the Web UI.
> The `/` route would likely query that data directly form that materialized table in SQL Server instead so that way the developer wouldn't have to re-invent (in Akka.NET) pagination and other niceties that are mostly built-into or built-on-top-of SQL tooling. 

### Why Build an Ordering / Inventory System with Akka.NET?

"I could do this all with CRUD!" you might say to yourself upon glancing at the domain. Imagine if you need to support any of the following requirements:

1. Send "low inventory" or "back order" warnings to the buyer team inside your company when inventory of a product is running low;
2. Automatically fulfill back-ordered orders when new supply for an exhausted product arrives;
3. Handling complex transactions where one order comprises of many products, some of which may be available and some of which may not (this is may require a [Saga or a Process Manager](https://petabridge.com/blog/akkadotnet-clusters-sagas/));
4. Being able to display the number of users currently looking at purchasing this product right now or the total that have been bought today; or
5. Dynamic pricing.

Implementing these scenarios with CRUD will necessitate:

1. Complex schema around inventory management;
2. Several round-trips to the database per-operation;
3. Read-after-write processes to dispatch notifications, most likely polling-driven or scheduled batch jobs - so warnings or notifications can't be done in anything resembling real-time; and
4. Some requirements, such as dynamic pricing, can't realistically be delivered using CRUD for a sufficiently high volume of demand and highly perishable inventory (i.e. plane tickets, flowers, hotel room stays, etc.)

Implementing this with actors instead gives us the following:

1. Data and business logic are adjacent to each other in-memory, so notification or saga workflows can be invoked immediately without waiting for a batch job;
2. All state changes to inventory or demand levels happen sequentially and in-memory, so implementing notifications or dynamic pricing is extremely inexpensive and fast;
3. Coordinating orders across multiple actors becomes an exercise in using a shared messaging workflow that can be easily tested and inexpensively executed, versus massive database queries; and
4. All actor state is still persisted, its schema is robustly versionable via Google Protocol Buffers, and can still be transformed, via actors designed similarly to the `ProductIndexActor`, into data rows in SQL Server that need to be quantifiable for reporting or business analysis purposes. In other words, the "business state" is a downstream projection of the "application state" - rather than trying to use the same dataset for both, which is typically what happens in CRUDworld.

### Key Technical Details

Here are the key details for understanding what this sample does and how it works:

1. **Akka.Cluster.Sharding does all of the heavy lifting** - it guarantees that for every unique `productID` there exists exactly one `ProductTotalsActor` that owns the state of that entity. That guarantee is upheld even across network splits. One `ProductTotalsActor` can be moved from one `SqlSharding.Sql.Host` node to another, message traffic to that actor will be paused while this happens, the actor will be recreated on its new home node, and message traffic will be resumed. The `WebApp` and `ProductIndexActor` instances have no idea that this is happening *and they don't need to*. In addition to that, the `ShardRegion` hosting the `ProductIndexActor` will dynamically create new instances of those actors on-demand and will, by default, kill existing instances of those actors if they haven't been sent a message for more than 120 seconds. You can change how the "kill off actors" behavior works, however: `akka.cluster.sharding.remember-entities=off` turns this off and keeps entity actors alive forever, or you can just change the time threshold via `akka.cluster.sharding.passivate-idle-entity-after = 400s`.
2. **`ShardRegionProxy` is how `SqlSharding.WebApp` communicates with `ProductTotalsActor`s hosted on the `SqlSharding.Sql.Host` instances** - again, the Akka.Cluster.Sharding infrastructure does most of the heavy work here; but this time from the perspective of the WebApp - all of the messages sent to the `/product/{productId}` route are ultimately routed to a `ShardRegionProxy` `IActorRef`. This actor knows how to communicate with the `ShardRegion` on the `SqlSharding.Sql.Host` role to dynamically instantiate a new `ProductTotalsActor` instance, if necessary, and route messages to it.
3. **Akka.Persistence is what we use to manage entity state, and we use event-sourcing to provide long-term extensibility** - the separation between commands, events, and queries is an important feature of how this application is designed. Cleanly segmenting the "message grammar" helps keep things organized ultimately simplifies the stateful piece of our programming model: the state of any given entity must be derived from the sum of its events. We use Akka.Persistence.SqlServer to store / recover this data and we use Google.Protobuf to serialize events, state, commands, and queries in a highly versionable way.

## Running Sample

### Launch Dependencies

To run this sample, we first need to spin up our dependencies - a SQL Server instance with a predefined `Akka` database ready to be configured.

**Windows**

```shell
start-dependencies.cmd
```

**Linux or OS X**

```shell
./start-dependencies.sh
```

This will build a copy of our [MSSQL image](https://github.com/petabridge/akkadotnet-code-samples/tree/master/infrastructure/mssql) and run it on port 1533. The sample is already pre-configured to connect to it at startup.

> **N.B.** give the MSSQL image about 30 seconds to start up and check the logs to see if an `Akka` database was successfully created or not. You may have to restart the container if you see that it failed the first time as the initial database file system initialization overhead can take quite up to 45-60s sometimes. Restarting the container doesn't include any of this overhead as the database file system is now already instantiated inside the container's ephemeral storage.

### Run the Sample

On initial run, the database can be seeded by setting the `SEED_DB=true` environment variable

Load up Rider or Visual Studio and


1. Launch `SqlSharding.Sql.Host`, followed by
2. Launch `SqlSharding.WebApp`.

Provided that you don't see any SQL Server connection errors originating from `SqlSharding.Sql.Host` - you should have no trouble using the WebApp's UI to add products, submit orders, change inventory levels, and more.
