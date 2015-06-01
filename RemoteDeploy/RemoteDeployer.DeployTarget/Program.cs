using System;
using Akka.Actor;
using Akka.Configuration;

namespace RemoteDeployer.DeployTarget
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("DeployTarget", ConfigurationFactory.ParseString(@"
                akka {  
                    actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                    remote {
                        helios.tcp {
		                    port = 8090
		                    hostname = localhost
                        }
                    }
                }")))
            {
                Console.ReadKey();
            }
        }
    }
}
