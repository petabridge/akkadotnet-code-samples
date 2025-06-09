# Akka.NET Cluster Setup Using Microsoft .NET Aspire

The goal of this sample is to demonstrate how a three-node cluster can be set-up in a single machine using Microsoft .NET Aspire 

## Technology

This solution is built with:

- Minimal APIs
- Docker container
- [Akka.NET v1.5 w/ Akka.Cluster](https://github.com/akkadotnet/akka.net)
- [Akka.Hosting](https://github.com/akkadotnet/Akka.Hosting) - which minimizes the amount of configuration for Akka.NET to practically zero.
- [Microsoft .NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)

## Domain

There is no domain for this example, it is set up to show a bare-bone minimum code required to run a three node cluster using Aspire.

## Running Sample

Load up Rider or Visual Studio and launch `SimpleCluster.AppHost`

You should see
1. Aspire dashboard opened in your browser.
    * If the dashboard does not open automatically, go to this address: `http://localhost:15258`
2. All resource status shown in the dashboard
    * The `azure` resource should be in "Unhealthy" state until Azurite Docker container is successfully downloaded and run, where it should switch to the "Running" state
    * Three `akka-node` resource which should be in "Waiting" state and switches to "Running" state after the `azure` resource is running.
3. Cluster bootstrap creates the cluster
    * Cluster should form in a few seconds. View the console log of one of the `akka-node` resource to see the cluster forming, you should see something similar to the logs below. 

```
[INFO][06/09/2025 19:40:14.779Z][Thread 0020][akka.tcp://SimpleCluster@localhost:56545/system/bootstrapCoordinator] Looking up [Lookup(akka-discovery, , tcp)]
[INFO][06/09/2025 19:40:14.785Z][Thread 0004][akka.tcp://SimpleCluster@localhost:56545/system/bootstrapCoordinator] Located service members based on: [Lookup(akka-discovery, , tcp)]: [ResolvedTarget(localhost, 56543, ), ResolvedTarget(localhost, 56544, ), ResolvedTarget(localhost, 56549, )], filtered to [localhost:56543, localhost:56544, localhost:56549]
[INFO][06/09/2025 19:40:15.155Z][Thread 0004][akka.tcp://SimpleCluster@localhost:56545/system/bootstrapCoordinator] Contact point [akka.tcp://SimpleCluster@localhost:56548] returned [1] seed-nodes [akka.tcp://SimpleCluster@localhost:56548]
[INFO][06/09/2025 19:40:15.156Z][Thread 0004][akka.tcp://SimpleCluster@localhost:56545/system/bootstrapCoordinator] Joining [akka.tcp://SimpleCluster@localhost:56545] to existing cluster [akka.tcp://SimpleCluster@localhost:56548]
[INFO][06/09/2025 19:40:15.295Z][Thread 0012][Cluster (akka://SimpleCluster)] Cluster Node [akka.tcp://SimpleCluster@localhost:56545] - Welcome from [akka.tcp://SimpleCluster@localhost:56548]
```