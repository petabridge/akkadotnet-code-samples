# Akka.NET Actor System intercommunication

The goal of this sample is to demonstrate how an external actor system can communicate with a cluster

## Technology

This solution is built with:

- Minimal APIs;
- C# `record` types;
- Akka.NET v1.5 w/ Akka.Cluster;
- Akka.Cluster.Tools; and
- [Akka.Hosting](https://github.com/akkadotnet/Akka.Hosting) - which minimizes the amount of configuration for Akka.NET to practically zero.


## Domain

Like all the samples in this repository, we are using a simple domain since the focus of the sample is meant to be _how to use Akka.NET infrastructure succinctly and successfully_. That being said, it's still worth understanding what our domain does: work distribution and metric collection.

### Backend

The backend app consists of two Akka.Cluster role types:

1. `ClusterClientSample.Gateway` with role "gateway" - a [headless Akka.NET process](https://petabridge.com/blog/akkadotnet-ihostedservice/) that acts as the known cluster client receptionist node (fixed address) that accepts work and metric report requests and
2. `ClusterClientSample.Node` with role "worker" - a [headless Akka.NET process](https://petabridge.com/blog/akkadotnet-ihostedservice/) where all worker actors were to be distributed.

Actor types in the backend cluster nodes:
* `WorkerManagerActor` - This actor is responsible for
  * transforming the `BatchedWork` message into `Work` messages and forwarding them to the distributed `WorkerActor`s in the cluster, and 
  * creating the cluster router that will deploy and route all `Work` messages to the `WorkerActor`s inside the cluster.

  It is registered to `ClusterClientReceptionist` as a service with its actor path **"/user/worker-manager"** as the service key for other actor systems to access.
* `MetricCounterActor` - This actor is responsible for keeping track of works done by each of the `WorkerActor`s. It is registered to `ClusterClientReceptionist` as a subscriber to the **"report"** topic.
* `WorkerActor` - This actor is responsible for the asynchronous business logic execution of the `Work` messages. The `WorkerActor` type is shared using a shared `ClusterClientSample.Shared` library so that it can be used by both the `ClusterClientSample.Gateway` and `ClusterClientSample.Node` projects.

## Frontend

The frontend app `ClusterClientSample.FrontEnd` is a simple [headless Akka.NET process](https://petabridge.com/blog/akkadotnet-ihostedservice/) with Akka.Remote that periodically sends messages to the backend app via the `ClusterClient` tunneling.

We have only two actor types in the frontend app:
* `BatchedWorkRequester` - This actor periodically
  * sends a `BatchedWork` request message to the backend app **"/user/worker-manager" service** via `ClusterClient`, and
  * receives the `Result` messages sent back after a work has been completed and logs it.
* `WorkReportCollector` - This actor
  * publishes a `SendReport` request message to the backend app **"report" pub-sub topic** via `ClusterClient`, and
  * receives the `Report` message sent by the metric actor and logs it.

## Running Sample

Load up Rider or Visual Studio and

1. Launch `ClusterClientSample.Gateway`, followed by
2. Launch `ClusterClientSample.Node`, and then
3. Launch `ClusterClientSample.FrontEnd`

You should see
1. Distributed work scheduling:
   * The frontend app sending batched work requests to the backend gateway node
   * The worker actors in the worker node completing each work requests asynchronously
   * The result for all completed work to be sent back to the frontend app and printed in the console.
2. Workload metric reporting:
   * The frontend app publishing metric requests to the pub-sub topic in the backend gateway node
   * The metric report sent back to the frontend app and printed in the console. 