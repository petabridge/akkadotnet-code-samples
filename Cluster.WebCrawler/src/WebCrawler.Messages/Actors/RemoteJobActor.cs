using Akka.Actor;
using WebCrawler.Messages.Commands;
using WebCrawler.Messages.Commands.V1;

namespace WebCrawler.Messages.Actors
{
    /// <summary>
    /// Remote-deployed actor designed to help forward jobs to the remote hosts
    /// </summary>
    public class RemoteJobActor : ReceiveActor
    {
        public RemoteJobActor()
        {
            Receive<IStartJobV1>(start =>
            {
                Context.ActorSelection("/user/api").Tell(start, Sender);
            });
        }
    }
}
