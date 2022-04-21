using Akka.Actor;
using Akka.Event;
using Akka.Util;
using ReliableRabbitMQ.Shared.Messages;

namespace ReliableRabbitMQ.Consumer.Actors;

/// <summary>
/// Sharded entity actor for product orders.
/// </summary>
/// <remarks>
/// Not using Akka.Persistence here in order to keep the sample complexity low.
/// </remarks>
public sealed class ProductActor : ReceiveActor
{
    public static Props CreateProps(string productId)
    {
        return Props.Create(() => new ProductActor(productId));
    }
    
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly HashSet<CreateOrder> _orders = new HashSet<CreateOrder>();
    private readonly HashSet<CreateOrder> _rejectedOrders = new HashSet<CreateOrder>();

    public int TotalInventoryOrdered => _orders.Sum(c => c.Quantity);

    /// <summary>
    /// Simulate a network failure randomly, in order to put re-delivery pressure back onto RabbitMQ
    /// </summary>
    public bool SimulateNetworkFailure => ThreadLocalRandom.Current.Next(0, 10) % 2 == 0;
    
    private readonly string _productId;

    public ProductActor(string productId)
    {
        _productId = productId;

        Receive<CreateOrder>(o =>
        {
            if (SimulateNetworkFailure)
            {
                _log.Warning("Network failure! Dropping order {0} from product [{1}]", o, _productId);
                _rejectedOrders.Add(o);
                return; // send nothing back. Let the system timeout.
            }

            if (_rejectedOrders.Contains(o))
            {
                _log.Info("Previously failed order {0} for product [{1}] can now be successfully processed", o, _productId);
            }
            _orders.Add(o);
            _log.Info("Added order {0} for product [{1}] - {2} total quantity ordered", o, _productId, TotalInventoryOrdered);
            Sender.Tell(new OrderCommandAck(o.OrderId, o.ProductId));
        });
    }
}