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
using FetchAllProductsResponse = SqlSharding.Shared.Queries.FetchAllProductsResponse;
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
    public const string FetchProductManifest = "fp";
    public const string FetchProductResultManifest = "fpr";

    /// <summary>
    /// Unique value greater than 100 as [0-100] is reserved for Akka.NET System serializers. 
    /// </summary>
    public override int Identifier => 556; //(int serializerId, string manifest)

    public override byte[] ToBinary(object obj)
    {
        switch (obj)
        {
            case Commands.ProductCommandResponse pcr:
                return ToProto(pcr).ToByteArray();
            case ProductState ps:
                return ToProto(ps).ToByteArray();
            case FetchProduct fp:
                return ToProto(fp).ToByteArray();
            case FetchResult fr:
                return ToProto(fr.State).ToByteArray();
            case FetchAllProducts _:
                return Array.Empty<byte>();
            case FetchAllProductsResponse rsp:
                return ToProto(rsp).ToByteArray();
            case ProductOrder po:
                return ToProto(po).ToByteArray();
            case Commands.PurchaseProduct pp:
                return ToProto(pp).ToByteArray();
            case ProductSold ps:
                return ToProto(ps).ToByteArray();
            case ProductCreated pc:
                return ToProto(pc).ToByteArray();
            case CreateProduct cp:
                return ToProto(cp).ToByteArray();
            case SupplyProduct sp:
                return ToProto(sp).ToByteArray();
            case ProductInventoryChanged ic:
                return ToProto(ic).ToByteArray();
            case ProductInventoryWarningEvent iw:
                return ToProto(iw).ToByteArray();
            default:
                throw new ArgumentOutOfRangeException(nameof(obj), $"Unsupported message type [{obj.GetType()}]");
        }
    }

    public override object FromBinary(byte[] bytes, string manifest)
    {
        switch (manifest)
        {
            case ProductCommandResponseManifest:
                return FromProto(Proto.ProductCommandResponse.Parser.ParseFrom(bytes));
            case ProductStateManifest:
                return FromProto(Proto.ProductState.Parser.ParseFrom(bytes));
            case FetchProductManifest:
                return FromProto(Proto.FetchProduct.Parser.ParseFrom(bytes));
            case FetchProductResultManifest:
                return new FetchResult(FromProto(Proto.ProductState.Parser.ParseFrom(bytes)));
            case FetchAllProductsManifest:
                return FetchAllProducts.Instance;
            case FetchAllProductsResponseManifest:
                return FromProto(Proto.FetchAllProductsResponse.Parser.ParseFrom(bytes));
            case ProductOrderManifest:
                return FromProto(Proto.ProductOrder.Parser.ParseFrom(bytes));
            case PurchaseProductManifest:
                return FromProto(Proto.PurchaseProduct.Parser.ParseFrom(bytes));
            case ProductSoldManifest:
                return FromProto(Proto.ProductSold.Parser.ParseFrom(bytes));
            case ProductCreatedManifest:
                return FromProto(Proto.ProductCreated.Parser.ParseFrom(bytes));
            case CreateProductManifest:
                return FromProto(Proto.CreateProduct.Parser.ParseFrom(bytes));
            case SupplyProductManifest:
                return FromProto(Proto.SupplyProduct.Parser.ParseFrom(bytes));
            case InventoryChangedManifest:
                return FromProto(Proto.InventoryChanged.Parser.ParseFrom(bytes));
            case InventoryWarningManifest:
                return FromProto(Proto.InventoryWarning.Parser.ParseFrom(bytes));
            default:
                throw new ArgumentOutOfRangeException(nameof(manifest), $"Unsupported message manifest [{manifest}]");
        }
    }

    public override string Manifest(object o)
    {
        switch (o)
        {
            case Commands.ProductCommandResponse _:
                return ProductCommandResponseManifest;
            case ProductState _:
                return ProductStateManifest;
            case FetchProduct _:
                return FetchProductManifest;
            case FetchResult _:
                return FetchProductResultManifest;
            case FetchAllProducts _:
                return FetchAllProductsManifest;
            case FetchAllProductsResponse _:
                return FetchAllProductsResponseManifest;
            case ProductOrder _:
                return ProductOrderManifest;
            case Commands.PurchaseProduct _:
                return PurchaseProductManifest;
            case ProductSold _:
                return ProductSoldManifest;
            case ProductCreated _:
                return ProductCreatedManifest;
            case CreateProduct _:
                return CreateProductManifest;
            case SupplyProduct _:
                return SupplyProductManifest;
            case ProductInventoryChanged _:
                return InventoryChangedManifest;
            case ProductInventoryWarningEvent _:
                return InventoryWarningManifest;
            default:
                throw new ArgumentOutOfRangeException(nameof(o), $"Unsupported message type [{o.GetType()}]");
        }
    }

    private static Proto.FetchProduct ToProto(Queries.FetchProduct purchase)
    {
        var proto = new Proto.FetchProduct();
        proto.ProductId = purchase.ProductId;
        return proto;
    }

    private static Queries.FetchProduct FromProto(Proto.FetchProduct proto)
    {
        return new Queries.FetchProduct(proto.ProductId);
    }

    private static FetchAllProductsResponse FromProto(Proto.FetchAllProductsResponse protoPurchase)
    {
        return new FetchAllProductsResponse(protoPurchase.Products.Select(c => FromProto(c)).ToList());
    }

    private static Proto.FetchAllProductsResponse ToProto(FetchAllProductsResponse purchase)
    {
        var proto = new Proto.FetchAllProductsResponse();
        proto.Products.AddRange(purchase.Products.Select(c => ToProto(c)));
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
        var proto = new Proto.CreateProduct();
        proto.Price = purchase.Price.FromDecimal();
        proto.InitialQuantity = purchase.InitialQuantity;
        proto.ProductId = purchase.ProductId;
        proto.ProductName = purchase.ProductName;
        return proto;
    }

    private static CreateProduct FromProto(Proto.CreateProduct protoCreate)
    {
        var createProduct = new CreateProduct(protoCreate.ProductId, protoCreate.ProductName,
            protoCreate.Price.ToDecimal(), protoCreate.InitialQuantity);
        return createProduct;
    }

    private static Proto.PurchaseProduct ToProto(Commands.PurchaseProduct purchase)
    {
        var protoPurchase = new Proto.PurchaseProduct();
        protoPurchase.Order = ToProto(purchase.NewOrder);
        return protoPurchase;
    }

    private static Commands.PurchaseProduct FromProto(Proto.PurchaseProduct protoPurchase)
    {
        var purchase = new Commands.PurchaseProduct(FromProto(protoPurchase.Order));
        return purchase;
    }

    private static Proto.ProductState ToProto(ProductState state)
    {
        var protoState = new Proto.ProductState();
        protoState.Data = ToProto(state.Data);
        protoState.Orders.AddRange(state.Orders.Select(c => ToProto(c)));
        protoState.Warnings.AddRange(state.Warnings.Select(c => ToProto(c)));
        protoState.Totals = ToProto(state.Totals);
        protoState.InventoryChanges.AddRange(state.InventoryChanges.Select(c => ToProto(c)));
        return protoState;
    }

    private static ProductState FromProto(Proto.ProductState protoState)
    {
        var productState = new ProductState
        {
            Data = FromProto(protoState.Data), Totals = FromProto(protoState.Totals),
            Warnings = protoState.Warnings.Select(c => FromProto(c)).ToImmutableSortedSet(),
            Orders = protoState.Orders.Select(c => FromProto(c)).ToImmutableSortedSet(),
            InventoryChanges = protoState.InventoryChanges.Select(c => FromProto(c)).ToImmutableSortedSet()
        };

        return productState;
    }

    private static Proto.ProductCommandResponse ToProto(Commands.ProductCommandResponse pcr)
    {
        var rsp = new Proto.ProductCommandResponse();
        rsp.Events.AddRange(pcr.ResponseEvents.Select(c => ToProductEvent(c)));
        rsp.Message = pcr.Message;
        rsp.Success = pcr.Success;
        rsp.ProductId = pcr.ProductId;
        return rsp;
    }

    private static Commands.ProductCommandResponse FromProto(Proto.ProductCommandResponse pcr)
    {
        var rsp = new Commands.ProductCommandResponse(pcr.ProductId,
            pcr.Events.Select(c => FromProductEvent(c)).ToArray(), pcr.Success, pcr.Message);
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
            ProductId = warning.ProductId
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
        var protoSold = new Proto.ProductSold()
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
        var inventoryChanged = new Proto.InventoryChanged();
        var (productId, quantity, timestamp, inventoryChangeReason) = changed;
        inventoryChanged.Reason = ToProto(inventoryChangeReason);
        inventoryChanged.ProductId = productId;
        inventoryChanged.QuantityChanged = quantity;
        inventoryChanged.Timestamp = Timestamp.FromDateTime(timestamp.ToUniversalTime());
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
        return new Proto.ProductData()
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

    private static Proto.ProductTotals ToProto(PurchasingTotals totals)
    {
        var protoTotals = new Proto.ProductTotals();
        protoTotals.RemainingInventory = totals.RemainingInventory;
        protoTotals.TotalRevenue = totals.TotalRevenue.FromDecimal();
        protoTotals.UnitsSold = totals.SoldInventory;
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