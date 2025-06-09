using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

#region Setup Azure

var azureStorage = builder.AddAzureStorage("azure");

// use Azurite docker container to emulate Azure if we're in a development environment
if (builder.Environment.IsDevelopment())
    azureStorage.RunAsEmulator();

var azureTables = azureStorage.AddTables("azure-tables");

#endregion

#region Setup Akka

builder.AddProject<Projects.SimpleCluster_Node>("akka-node")
    // Spin up 3 replicas
    .WithReplicas(3)
    // Let Aspire inject the Azure table connection string
    .WithReference(azureTables)
    // Let Aspire assign a random port for Akka.Remote
    .WithEndpoint(name: "remote", env: "Akka__Remote_Port")
    // Let Aspire assign a random port for Akka.Management
    .WithEndpoint(name: "management", env: "Akka__Management_Port")
    // Make sure that Azure Tables is running before starting Akka nodes
    .WaitFor(azureTables);

#endregion

builder.Build().Run();