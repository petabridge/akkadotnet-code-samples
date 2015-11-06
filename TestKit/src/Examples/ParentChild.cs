using Akka.Actor;
using NUnit.Framework;
using Akka.TestKit.NUnit;

namespace TestKitSample.Examples
{
    public class ChildActor : ReceiveActor
    {
        public ChildActor()
        {
            ReceiveAny(o => Sender.Tell("hello!"));
        }
    }

    public class ParentActor : ReceiveActor
    {
        public ParentActor()
        {
            var child = Context.ActorOf(Props.Create(() => new ChildActor()));
            ReceiveAny(o => child.Forward(o));
        }
    }

    [TestFixture]
    public class ParentGreeterSpecs : TestKit
    {
        [Test]
        public void Parent_should_create_child()
        {
            // verify child has been created by sending parent a message
            // that is forwarded to child, and which child replies to sender with
            var parentProps = Props.Create(() => new ParentActor());
            var parent = ActorOfAsTestActorRef<ParentActor>(parentProps, TestActor);
            parent.Tell("this should be forwarded to the child");
            ExpectMsg("hello!");
        }
    }
}


