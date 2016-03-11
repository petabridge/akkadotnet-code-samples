namespace AtLeastOnceDelivery.Console
{
    public class ReliableDeliveryEnvelope<TMessage>
    {
        public ReliableDeliveryEnvelope(TMessage message, long messageId)
        {
            Message = message;
            MessageId = messageId;
        }

        public TMessage Message { get; private set; }

        public long MessageId { get; private set; }
    }

    public class ReliableDeliveryAck
    {
        public ReliableDeliveryAck(long messageId)
        {
            MessageId = messageId;
        }

        public long MessageId { get; private set; }
    }

    public class Write
    {
        public Write(string content)
        {
            Content = content;
        }

        public string Content { get; private set; }
    }
}
