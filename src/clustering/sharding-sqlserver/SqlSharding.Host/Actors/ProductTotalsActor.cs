using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Event;
using Akka.Persistence;
using SqlSharding.Shared;
using SqlSharding.Shared.Commands;
using SqlSharding.Shared.Events;
using SqlSharding.Shared.Queries;

namespace SqlSharding.Host.Actors;

/// <summary>
/// Manages the state for a given product.
/// </summary>
public sealed class ProductTotalsActor : ReceivePersistentActor
{
    public static Props GetProps(string persistenceId)
    {
        return Props.Create(() => new ProductTotalsActor(persistenceId));
    }
    
    /// <summary>
    /// Used to help differentiate what type of entity this is inside Akka.Persistence's database
    /// </summary>
    public const string TotalsEntityNameConstant = "totals";

    private readonly ILoggingAdapter _log = Context.GetLogger();

    public ProductTotalsActor(string persistenceId)
    {
        PersistenceId = $"{TotalsEntityNameConstant}-" + persistenceId;
        State = new ProductState();
        
        Recover<SnapshotOffer>(offer =>
        {
            if (offer.Snapshot is ProductState state)
            {
                State = state;
            }
        });

        Recover<IProductEvent>(productEvent =>
        {
            State = State.ProcessEvent(productEvent);
        });

        Command<IProductCommand>(cmd =>
        {
            var response = State.ProcessCommand(cmd);
            var sentResponse = false;

            if (response.ResponseEvents.Any())
            {
                PersistAll(response.ResponseEvents, productEvent =>
                {
                    _log.Info("Processed: {0}", productEvent);
                
                    if (productEvent is ProductInventoryWarningEvent warning)
                    {
                        _log.Warning(warning.ToString());
                    }
                    State = State.ProcessEvent(productEvent);

                    if (!sentResponse) // otherwise we'll generate a response-per-event
                    {
                        sentResponse = true;

                        async Task<ProductCommandResponse> ReplyToSender()
                        {
                            await Task.Delay(1);
                            return response;
                        }

                        ReplyToSender().PipeTo(Sender, failure: ex => new Status.Failure(ex));

                    }
                
                    if(LastSequenceNr % 10 == 0)
                        SaveSnapshot(State);
                });
            }
            else
            {
                Sender.Tell(response);
            }
        });


        Command<SaveSnapshotSuccess>(success =>
        {
            
        });

        Command<FetchProduct>(fetch =>
        {
            Sender.Tell(new FetchResult(State));

            if (State.IsEmpty)
            {
                // we don't exist, so don't let `remember-entities` keep us alive
                Context.Parent.Tell(new Passivate(PoisonPill.Instance));
            }
        });
    }
    
    public override string PersistenceId { get; }
    
    public ProductState State { get; set; }
}