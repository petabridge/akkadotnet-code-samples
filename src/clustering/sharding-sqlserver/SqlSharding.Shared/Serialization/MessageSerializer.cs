using System.Collections.Immutable;
using Akka.Actor;
using Akka.Serialization;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Configuration;
using SqlSharding.Shared.Events;
using SqlSharding.Shared.Queries;
using SqlSharding.Shared.Serialization.Proto;
using CreateProduct = SqlSharding.Shared.Commands.CreateProduct;
using FetchAllProductsImpl = SqlSharding.Shared.Queries.FetchAllProductsImpl;
using FetchAllProductsResponse = SqlSharding.Shared.Queries.FetchAllProductsResponse;
using FetchSoldProductsImpl = SqlSharding.Shared.Queries.FetchSoldProductsImpl;
using FetchSoldProductsResponse = SqlSharding.Shared.Queries.FetchSoldProductsResponse;
using FetchWarningEventsImpl = SqlSharding.Shared.Queries.FetchWarningEventsImpl;
using FetchWarningEventsResponse = SqlSharding.Shared.Queries.FetchWarningEventsResponse;
using FetchProduct = SqlSharding.Shared.Queries.FetchProduct;
using InventoryChangeReason = SqlSharding.Shared.Events.InventoryChangeReason;
using ProductCreated = SqlSharding.Shared.Events.ProductCreated;
using ProductSold = SqlSharding.Shared.Events.ProductSold;
using ProductWarningReason = SqlSharding.Shared.Events.ProductWarningReason;
using SupplyProduct = SqlSharding.Shared.Commands.SupplyProduct;

namespace SqlSharding.Shared.Serialization;

public sealed class MessageSerializer : SerializerWithStringManifest
{
    public MessageSerializer(ExtendedActorSystem system) : base(system)
    {
    }

    public const string CreateProductManifest = "cp";
    public const string SupplyProductManifest = "sp";
    public const string PurchaseProductManifest = "pp";
    public const string ProductCommandResponseManifest = "pcr";

    public const string ProductStateManifest = "ps";
    public const string ProductOrderManifest = "po";
    public const string ProductSoldManifest = "prds";
    public const string ProductCreatedManifest = "pce";
    public const string InventoryChangedManifest = "ic";
    public const string InventoryWarningManifest = "iw";

    public const string FetchAllProductsManifest = "fall";
    public const string FetchAllProductsResponseManifest = "fallrsp";
    public const string FetchSoldProductsManifest = "fsld";
    public const string FetchSoldProductsResponseManifest = "fsldrsp";
    public const string FetchProductManifest = "fp";
    public const string FetchProductResultManifest = "fpr";
    public const string FetchWarningEventsManifest = "fwe";
    public const string FetchWarningEventsResponseManifest = "fwer";

    /// <summary>
    /// Unique value greater than 100 as [0-100] is reserved for Akka.NET System serializers. 
    /// </summary>
    public override int Identifier => 556; //(int serializerId, string manifest)

    public override byte[] ToBinary(object obj)
    {
        return obj switch
        {
            Commands.ProductCommandResponse pcr => ToProto(pcr).ToByteArray(),
            ProductState ps => ToProto(ps).ToByteArray(),
            FetchProduct fp => ToProto(fp).ToByteArray(),
            FetchResult fr => ToProto(fr.State).ToByteArray(),
            FetchAllProductsImpl fp => ToProto(fp).ToByteArray(),
            FetchAllProductsResponse rsp => ToProto(rsp).ToByteArray(),
            FetchSoldProductsImpl fs => ToProto(fs).ToByteArray(),
            FetchSoldProductsResponse rsp => ToProto(rsp).ToByteArray(),
            FetchWarningEventsImpl fwe => ToProto(fwe).ToByteArray(),
            FetchWarningEventsResponse rsp => ToProto(rsp).ToByteArray(),
            ProductOrder po => ToProto(po).ToByteArray(),
            Commands.PurchaseProduct pp => ToProto(pp).ToByteArray(),
            ProductSold ps => ToProto(ps).ToByteArray(),
            ProductCreated pc => ToProto(pc).ToByteArray(),
            CreateProduct cp => ToProto(cp).ToByteArray(),
            SupplyProduct sp => ToProto(sp).ToByteArray(),
            ProductInventoryChanged ic => ToProto(ic).ToByteArray(),
            ProductInventoryWarningEvent iw => ToProto(iw).ToByteArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(obj), $"Unsupported message type [{obj.GetType()}]")
        };
    }

    public override object FromBinary(byte[] bytes, string manifest)
    {
        return manifest switch
        {
            ProductCommandResponseManifest => FromProto(Proto.ProductCommandResponse.Parser.ParseFrom(bytes)),
            ProductStateManifest => FromProto(Proto.ProductState.Parser.ParseFrom(bytes)),
            FetchProductManifest => FromProto(Proto.FetchProduct.Parser.ParseFrom(bytes)),
            FetchProductResultManifest => new FetchResult(FromProto(Proto.ProductState.Parser.ParseFrom(bytes))),
            FetchAllProductsManifest => FromProto(Proto.FetchAllProductsImpl.Parser.ParseFrom(bytes)),
            FetchAllProductsResponseManifest => FromProto(Proto.FetchAllProductsResponse.Parser.ParseFrom(bytes)),
            FetchSoldProductsManifest => FromProto(Proto.FetchSoldProductsImpl.Parser.ParseFrom(bytes)),
            FetchSoldProductsResponseManifest => FromProto(Proto.FetchSoldProductsResponse.Parser.ParseFrom(bytes)),
            FetchWarningEventsManifest => FromProto(Proto.FetchWarningEventsImpl.Parser.ParseFrom(bytes)),
            FetchWarningEventsResponseManifest => FromProto(Proto.FetchWarningEventsResponse.Parser.ParseFrom(bytes)),
            ProductOrderManifest => FromProto(Proto.ProductOrder.Parser.ParseFrom(bytes)),
            PurchaseProductManifest => FromProto(Proto.PurchaseProduct.Parser.ParseFrom(bytes)),
            ProductSoldManifest => FromProto(Proto.ProductSold.Parser.ParseFrom(bytes)),
            ProductCreatedManifest => FromProto(Proto.ProductCreated.Parser.ParseFrom(bytes)),
            CreateProductManifest => FromProto(Proto.CreateProduct.Parser.ParseFrom(bytes)),
            SupplyProductManifest => FromProto(Proto.SupplyProduct.Parser.ParseFrom(bytes)),
            InventoryChangedManifest => FromProto(Proto.InventoryChanged.Parser.ParseFrom(bytes)),
            InventoryWarningManifest => FromProto(Proto.InventoryWarning.Parser.ParseFrom(bytes)),
            _ => throw new ArgumentOutOfRangeException(nameof(manifest), $"Unsupported message manifest [{manifest}]")
        };
    }

    public override string Manifest(object o)
    {
        return o switch
        {
            Commands.ProductCommandResponse => ProductCommandResponseManifest,
            ProductState => ProductStateManifest,
            FetchProduct => FetchProductManifest,
            FetchResult => FetchProductResultManifest,
            FetchAllProductsImpl => FetchAllProductsManifest,
            FetchAllProductsResponse => FetchAllProductsResponseManifest,
            FetchSoldProductsImpl => FetchSoldProductsManifest,
            FetchSoldProductsResponse => FetchSoldProductsResponseManifest,
            FetchWarningEventsImpl => FetchWarningEventsManifest,
            FetchWarningEventsResponse => FetchWarningEventsResponseManifest,
            ProductOrder => ProductOrderManifest,
            Commands.PurchaseProduct => PurchaseProductManifest,
            ProductSold => ProductSoldManifest,
            ProductCreated => ProductCreatedManifest,
            CreateProduct => CreateProductManifest,
            SupplyProduct => SupplyProductManifest,
            ProductInventoryChanged => InventoryChangedManifest,
            ProductInventoryWarningEvent => InventoryWarningManifest,
            _ => throw new ArgumentOutOfRangeException(nameof(o), $"Unsupported message type [{o.GetType()}]")
        };
    }

    private FetchAllProductsImpl FromProto(Proto.FetchAllProductsImpl protoPurchase)
    {
        return new FetchAllProductsImpl(protoPurchase.ProducerId, ResolveActorRef(protoPurchase.ActorRefPath));
    }
    
    private IActorRef ResolveActorRef(string path)
    {
        return system.Provider.ResolveActorRef(path);
    }
    
    private static Proto.FetchAllProductsImpl ToProto(FetchAllProductsImpl purchase)
    {
        return new Proto.FetchAllProductsImpl()
        {
            ProducerId = purchase.ProducerId,
            ActorRefPath = Akka.Serialization.Serialization.SerializedActorPath(purchase.ConsumerController)
        };
    }

    private FetchSoldProductsImpl FromProto(Proto.FetchSoldProductsImpl protoCommand)
    {
        return new FetchSoldProductsImpl(protoCommand.ProducerId, ResolveActorRef(protoCommand.ActorRefPath));
    }

    private static Proto.FetchSoldProductsImpl ToProto(FetchSoldProductsImpl command)
    {
        return new Proto.FetchSoldProductsImpl
        {
            ProducerId = command.ProducerId,
            ActorRefPath = Akka.Serialization.Serialization.SerializedActorPath(command.ConsumerController)
        };
    }
    
    private FetchWarningEventsImpl FromProto(Proto.FetchWarningEventsImpl protoCommand)
    {
        return new FetchWarningEventsImpl(protoCommand.ProducerId, ResolveActorRef(protoCommand.ActorRefPath));
    }

    private static Proto.FetchWarningEventsImpl ToProto(FetchWarningEventsImpl command)
    {
        return new Proto.FetchWarningEventsImpl
        {
            ProducerId = command.ProducerId,
            ActorRefPath = Akka.Serialization.Serialization.SerializedActorPath(command.ConsumerController)
        };
    }
    
    private static Proto.FetchProduct ToProto(Queries.FetchProduct purchase)
    {
        var proto = new Proto.FetchProduct
        {
            ProductId = purchase.ProductId
        };
        return proto;
    }

    private static Queries.FetchProduct FromProto(Proto.FetchProduct proto)
    {
        return new Queries.FetchProduct(proto.ProductId);
    }

    private static FetchAllProductsResponse FromProto(Proto.FetchAllProductsResponse protoPurchase)
    {
        return new FetchAllProductsResponse(protoPurchase.Products.Select(FromProto).ToList());
    }

    private static Proto.FetchAllProductsResponse ToProto(FetchAllProductsResponse purchase)
    {
        var proto = new Proto.FetchAllProductsResponse();
        proto.Products.AddRange(purchase.Products.Select(ToProto));
        return proto;
    }
    
    private static FetchSoldProductsResponse FromProto(Proto.FetchSoldProductsResponse response)
    {
        return new FetchSoldProductsResponse(response.Products.Select(FromProto).ToList());
    }
    
    private static Proto.FetchSoldProductsResponse ToProto(FetchSoldProductsResponse purchase)
    {
        var proto = new Proto.FetchSoldProductsResponse();
        proto.Products.AddRange(purchase.Products.Select(ToProto));
        return proto;
    }

    private static FetchWarningEventsResponse FromProto(Proto.FetchWarningEventsResponse response)
    {
        return new FetchWarningEventsResponse(response.Warnings.Select(FromProto).ToList());
    }
    
    private static Proto.FetchWarningEventsResponse ToProto(FetchWarningEventsResponse purchase)
    {
        var proto = new Proto.FetchWarningEventsResponse();
        proto.Warnings.AddRange(purchase.Warnings.Select(ToProto));
        return proto;
    }

    private static Proto.SupplyProduct ToProto(SupplyProduct purchase)
    {
        var proto = new Proto.SupplyProduct();
        proto.AdditionalQuantity = purchase.AdditionalQuantity;
        proto.ProductId = purchase.ProductId;
        return proto;
    }

    private static SupplyProduct FromProto(Proto.SupplyProduct proto)
    {
        var supply = new SupplyProduct(proto.ProductId, proto.AdditionalQuantity);
        return supply;
    }

    private static Proto.CreateProduct ToProto(CreateProduct purchase)
    {
        var proto = new Proto.CreateProduct
        {
            Price = purchase.Price.FromDecimal(),
            InitialQuantity = purchase.InitialQuantity,
            ProductId = purchase.ProductId,
            ProductName = purchase.ProductName
        };
        proto.Tags.AddRange(purchase.Tags);
        return proto;
    }

    private static CreateProduct FromProto(Proto.CreateProduct protoCreate)
    {
        var createProduct = new CreateProduct(protoCreate.ProductId, protoCreate.ProductName,
            protoCreate.Price.ToDecimal(), protoCreate.InitialQuantity, protoCreate.Tags.ToArray());
        return createProduct;
    }

    private static Proto.PurchaseProduct ToProto(Commands.PurchaseProduct purchase)
    {
        var protoPurchase = new Proto.PurchaseProduct
        {
            Order = ToProto(purchase.NewOrder)
        };
        return protoPurchase;
    }

    private static Commands.PurchaseProduct FromProto(Proto.PurchaseProduct protoPurchase)
    {
        var purchase = new Commands.PurchaseProduct(FromProto(protoPurchase.Order));
        return purchase;
    }

    private static Proto.ProductState ToProto(ProductState state)
    {
        var protoState = new Proto.ProductState
        {
            Data = ToProto(state.Data),
            Totals = ToProto(state.Totals)
        };
        protoState.Orders.AddRange(state.Orders.Select(ToProto));
        protoState.Warnings.AddRange(state.Warnings.Select(ToProto));
        protoState.InventoryChanges.AddRange(state.InventoryChanges.Select(ToProto));
        return protoState;
    }

    private static ProductState FromProto(Proto.ProductState protoState)
    {
        var productState = new ProductState
        {
            Data = FromProto(protoState.Data), Totals = FromProto(protoState.Totals),
            Warnings = protoState.Warnings.Select(FromProto).ToImmutableSortedSet(),
            Orders = protoState.Orders.Select(FromProto).ToImmutableSortedSet(),
            InventoryChanges = protoState.InventoryChanges.Select(FromProto).ToImmutableSortedSet()
        };

        return productState;
    }

    private static Proto.ProductCommandResponse ToProto(Commands.ProductCommandResponse pcr)
    {
        var rsp = new Proto.ProductCommandResponse
        {
            Message = pcr.Message,
            Success = pcr.Success,
            ProductId = pcr.ProductId
        };
        rsp.Events.AddRange(pcr.ResponseEvents.Select(ToProductEvent));
        return rsp;
    }

    private static Commands.ProductCommandResponse FromProto(Proto.ProductCommandResponse pcr)
    {
        var rsp = new Commands.ProductCommandResponse(pcr.ProductId,
            pcr.Events.Select(FromProductEvent).ToArray(), pcr.Success, pcr.Message);
        return rsp;
    }

    private static ProductEvent ToProductEvent(IProductEvent e)
    {
        var productEvent = new ProductEvent();
        switch (e)
        {
            case ProductCreated created:
                productEvent.ProductCreated = ToProto(created);
                break;
            case ProductInventoryChanged changed:
                productEvent.InventoryChanged = ToProto(changed);
                break;
            case ProductSold sold:
                productEvent.ProductSold = ToProto(sold);
                break;
            case ProductInventoryWarningEvent warning:
                productEvent.InventoryWarning = ToProto(warning);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e), e, null);
        }

        return productEvent;
    }

    private static IProductEvent FromProductEvent(ProductEvent e)
    {
        if (e.InventoryChanged != null)
        {
            return FromProto(e.InventoryChanged);
        }

        if (e.InventoryWarning != null)
        {
            return FromProto(e.InventoryWarning);
        }

        if (e.ProductCreated != null)
        {
            return FromProto(e.ProductCreated);
        }

        if (e.ProductSold != null)
        {
            return FromProto(e.ProductSold);
        }

        throw new ArgumentException("Did not find matching CLR type for ProductEvent");
    }

    private static InventoryWarning ToProto(ProductInventoryWarningEvent warning)
    {
        var protoWarning = new InventoryWarning
        {
            Reason = ToProto(warning.Reason),
            Timestamp = warning.Timestamp.ToTimestamp(),
            ProductId = warning.ProductId,
            Message = warning.Message
        };

        return protoWarning;
    }

    private static ProductInventoryWarningEvent FromProto(InventoryWarning protoWarning)
    {
        var warning = new ProductInventoryWarningEvent(protoWarning.ProductId, FromProto(protoWarning.Reason),
            protoWarning.Timestamp.ToDateTime(), protoWarning.Message);
        return warning;
    }

    private static Proto.ProductWarningReason ToProto(ProductWarningReason warning)
    {
        return warning switch
        {
            ProductWarningReason.LowSupply => Proto.ProductWarningReason.LowSupply,
            ProductWarningReason.NoSupply => Proto.ProductWarningReason.NoSupply,
            _ => throw new ArgumentOutOfRangeException(nameof(warning), warning, null)
        };
    }

    private static ProductWarningReason FromProto(Proto.ProductWarningReason warning)
    {
        return warning switch
        {
            Proto.ProductWarningReason.LowSupply => ProductWarningReason.LowSupply,
            Proto.ProductWarningReason.NoSupply => ProductWarningReason.NoSupply,
            _ => throw new ArgumentOutOfRangeException(nameof(warning), warning, null)
        };
    }

    private static Proto.ProductSold ToProto(ProductSold changed)
    {
        var protoSold = new Proto.ProductSold
        {
            Backordered = changed.BackOrdered,
            UnitPrice = changed.UnitPrice.FromDecimal(),
            Order = ToProto(changed.Order)
        };

        return protoSold;
    }

    private static ProductSold FromProto(Proto.ProductSold protoSold)
    {
        var sold = new ProductSold(FromProto(protoSold.Order), protoSold.UnitPrice.ToDecimal(), protoSold.Backordered);
        return sold;
    }

    private static Proto.ProductOrder ToProto(ProductOrder order)
    {
        var protoOrder = new Proto.ProductOrder()
        {
            OrderId = order.OrderId,
            ProductId = order.ProductId,
            Quantity = order.Quantity,
            Timestamp = Timestamp.FromDateTime(order.Timestamp)
        };

        return protoOrder;
    }

    private static ProductOrder FromProto(Proto.ProductOrder order)
    {
        var po = new ProductOrder(order.OrderId, order.ProductId, order.Quantity, order.Timestamp.ToDateTime());
        return po;
    }

    private static InventoryChanged ToProto(ProductInventoryChanged changed)
    {
        var (productId, quantity, timestamp, inventoryChangeReason) = changed;
        var inventoryChanged = new Proto.InventoryChanged
        {
            Reason = ToProto(inventoryChangeReason),
            ProductId = productId,
            QuantityChanged = quantity,
            Timestamp = Timestamp.FromDateTime(timestamp.ToUniversalTime())
        };
        return inventoryChanged;
    }

    private static ProductInventoryChanged FromProto(InventoryChanged changed)
    {
        return new ProductInventoryChanged(changed.ProductId, changed.QuantityChanged, changed.Timestamp?.ToDateTime() ?? DateTime.MinValue, FromProto(changed.Reason));
    }

    private static Proto.InventoryChangeReason ToProto(InventoryChangeReason reason)
    {
        return reason switch
        {
            InventoryChangeReason.Fulfillment => Proto.InventoryChangeReason.Fulfillment,
            InventoryChangeReason.SupplyIncrease => Proto.InventoryChangeReason.SupplyIncrease,
            InventoryChangeReason.Lost => Proto.InventoryChangeReason.Lost,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };
    }

    private static InventoryChangeReason FromProto(Proto.InventoryChangeReason reason)
    {
        return reason switch
        {
            Proto.InventoryChangeReason.Fulfillment => InventoryChangeReason.Fulfillment,
            Proto.InventoryChangeReason.SupplyIncrease => InventoryChangeReason.SupplyIncrease,
            Proto.InventoryChangeReason.Lost => InventoryChangeReason.Lost,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };
    }

    private static Proto.ProductCreated ToProto(ProductCreated created)
    {
        var productCreated = new Proto.ProductCreated
        {
            Data = CreateProductData(created)
        };
        return productCreated;
    }

    private static ProductCreated FromProto(Proto.ProductCreated created)
    {
        return new ProductCreated(created.Data.ProductId, created.Data.ProductName, created.Data.Price.ToDecimal());
    }

    private static Proto.ProductData CreateProductData(ProductCreated created)
    {
        return new Proto.ProductData
        {
            Price = created.Price.FromDecimal(),
            ProductId = created.ProductId,
            ProductName = created.ProductName
        };
    }

    private static ProductData FromProto(Proto.ProductData data)
    {
        return new ProductData(data.ProductId, data.ProductName, data.Price.ToDecimal());
    }

    private static Proto.ProductData ToProto(ProductData data)
    {
        var protoData = new Proto.ProductData
        {
            Price = data.CurrentPrice.FromDecimal(),
            ProductId = data.ProductId,
            ProductName = data.ProductName
        };
        return protoData;
    }

    private static ProductsSoldData FromProto(Proto.ProductsSoldData data)
    {
        return new ProductsSoldData(FromProto(data.ProductData), data.Invoices.Select(FromProto).ToImmutableList());
    }
    
    private static Proto.ProductsSoldData ToProto(ProductsSoldData data)
    {
        var protoData = new Proto.ProductsSoldData
        {
            ProductData = ToProto(data.ProductData)
        };
        protoData.Invoices.AddRange(data.Invoices.Select(ToProto));
        return protoData;
    }
    
    private static WarningEventData FromProto(Proto.WarningEventData data)
    {
        return new WarningEventData(FromProto(data.ProductData), data.Warnings.Select(FromProto).ToImmutableSortedSet());
    }
    
    private static Proto.WarningEventData ToProto(WarningEventData data)
    {
        var protoData = new Proto.WarningEventData
        {
            ProductData = ToProto(data.ProductData)
        };
        protoData.Warnings.AddRange(data.Warnings.Select(ToProto));
        return protoData;
    }
    
    private static Proto.ProductTotals ToProto(PurchasingTotals totals)
    {
        var protoTotals = new Proto.ProductTotals
        {
            RemainingInventory = totals.RemainingInventory,
            TotalRevenue = totals.TotalRevenue.FromDecimal(),
            UnitsSold = totals.SoldInventory
        };
        return protoTotals;
    }

    private static PurchasingTotals FromProto(Proto.ProductTotals protoTotals)
    {
        var purchasingTotals = new PurchasingTotals(protoTotals.RemainingInventory, protoTotals.UnitsSold,
            protoTotals.TotalRevenue.ToDecimal());
        return purchasingTotals;
    }
}

public static class ProtoSerializationExtensions
{
    private const decimal NanoFactor = 1_000_000_000;

    public static decimal ToDecimal(this DecimalValue grpcDecimal)
    {
        return grpcDecimal.Units + grpcDecimal.Nanos / NanoFactor;
    }

    public static DecimalValue FromDecimal(this decimal value)
    {
        var units = decimal.ToInt64(value);
        var nanos = decimal.ToInt32((value - units) * NanoFactor);
        return new DecimalValue() { Units = units, Nanos = nanos };
    }
}