namespace AtLeastOnceDelivery.Console
{
    using System;
    using Akka.Actor;

    public class MyRecipientActor : ReceiveActor
    {
        public MyRecipientActor()
        {
            Receive<ReliableDeliveryEnvelope<Write>>(write =>
            {
                Console.WriteLine("Received message {0} [id: {1}] from {2} - accept?", write.Message.Content, write.MessageId, Sender);
                var response = Console.ReadLine()?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(response) && (response.Equals("yes") || response.Equals("y")))
                {
                    // confirm delivery only if the user explicitly agrees
                    Sender.Tell(new ReliableDeliveryAck(write.MessageId));
                    Console.WriteLine("Confirmed delivery of message ID {0}", write.MessageId);
                }
                else
                {
                    Console.WriteLine("Did not confirm delivery of message ID {0}", write.MessageId);
                }
            });
        }
    }
}