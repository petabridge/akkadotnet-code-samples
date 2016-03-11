namespace AtLeastOnceDelivery.Console
{
    using Akka.Actor;

    class Program
    {
        static void Main(string[] args)
        {
            using (var actorSystem = ActorSystem.Create("AtLeastOnceDeliveryDemo"))
            {
                var recipientActor = actorSystem.ActorOf(Props.Create(() => new MyRecipientActor()), "receiver");
                var atLeastOnceDeliveryActor =
                    actorSystem.ActorOf(Props.Create(() => new MyAtLeastOnceDeliveryActor(recipientActor)), "delivery");

                actorSystem.WhenTerminated.Wait();
            }
        }
    }
}
