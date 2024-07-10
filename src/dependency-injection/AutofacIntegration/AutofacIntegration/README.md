# Autofac And Akka.Hosting Integration

The goal of this sample is to demonstrate how to integrate `Autofac.Extensions.DependencyInjection` with `Akka.Hosting`.

## Technology

* Akka.NET v1.5
* Microsoft.Extensions.Hosting
* Autofac.Extensions.DependencyInjection
* Akka.DependencyInjection
* Akka.Hosting - which minimizes the amount of configuration for Akka.NET to practically zero

## Domain

The sample shows how two actors that uses constructor argument injection in their constructor and at least one of the argument is set up through Autofac.

## Running Sample

Load the solution into Rider or Visual Studio and run the project.
You should see the `EchoActor` actor logging the `AutofacInjected.TestString` read only property on the console and both `TestActor` and `EchoActor` communicating back and forth. 