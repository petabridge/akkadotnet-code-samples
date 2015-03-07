using Akka.Actor;
using WebCrawler.Messages.Commands;

namespace WebCrawler.Messages.Actors
{
    /// <summary>
    /// Remote-deployed actor designed to help forward jobs to the remote hosts
    /// </summary>
    public class RemoteJobActor : ReceiveActor
    {
        public RemoteJobActor()
        {
            Receive<StartJob>(start =>
            {
                Context.ActorSelection("/user/api").Tell(start, Sender);
            });
        }
    }
}
