using Akka.Actor;
using Akka.Serialization;
using SqlSharding.Shared.Events;
using SqlSharding.Shared.Serialization.Proto;
using InventoryChangeReason = SqlSharding.Shared.Events.InventoryChangeReason;
using ProductCreated = SqlSharding.Shared.Events.ProductCreated;
using ProductSold = SqlSharding.Shared.Events.ProductSold;

namespace SqlSharding.Shared.Serialization;

public sealed class MessageSerializer : SerializerWithStringManifest
{
    public MessageSerializer(ExtendedActorSystem system) : base(system)
    {
    }

    public const string CreateProductMainfest = "cp";
    public const string SupplyProductManifest = "sp";
    public const string PurchaseProductManifest = "pp";
    public const string ProductCommandResponseManifest = "pcr";

    public const string ProductStateManifest = "ps";
    public const string ProductOrderManifest = "po";
    public const string ProductSoldManifest = "prds";
    public const string ProductCreatedManifest = "pce";
    public const string InventoryChangedManifest = "ic";
    public const string InventoryWarningManifest = "iw";

    public const string ProductEventManifest = "pe";
    

    public override byte[] ToBinary(object obj)
    {
        switch (obj)
        {
            case ProductCommandResponse pcr:
                return ToProto(pcr);
            case ProductState _:
                return ProductStateManifest;
            case ProductOrder _:
                return ProductOrderManifest;
            case PurchaseProduct _:
                return PurchaseProductManifest;
            case ProductSold _:
                return ProductSoldManifest;
            case ProductCreated _:
                return ProductCreatedManifest;
            case CreateProduct _:
                return CreateProductMainfest;
            case SupplyProduct _:
                return SupplyProductManifest;
            case InventoryChanged _:
                return InventoryChangedManifest;
            case InventoryWarning _:
                return InventoryWarningManifest;
            default:
                throw new ArgumentOutOfRangeException(nameof(o), $"Unsupported message type [{o.GetType()}]");
        }
    }

    public override object FromBinary(byte[] bytes, string manifest)
    {
        throw new NotImplementedException();
    }

    public override string Manifest(object o)
    {
        switch (o)
        {
            case ProductCommandResponse _:
                return ProductCommandResponseManifest;
            case ProductState _:
                return ProductStateManifest;
            case ProductOrder _:
                return ProductOrderManifest;
            case PurchaseProduct _:
                return PurchaseProductManifest;
            case ProductSold _:
                return ProductSoldManifest;
            case ProductCreated _:
                return ProductCreatedManifest;
            case CreateProduct _:
                return CreateProductMainfest;
            case SupplyProduct _:
                return SupplyProductManifest;
            case InventoryChanged _:
                return InventoryChangedManifest;
            case InventoryWarning _:
                return InventoryWarningManifest;
            default:
                throw new ArgumentOutOfRangeException(nameof(o), $"Unsupported message type [{o.GetType()}]");
        }
    }
    
    private byte[] ToProto(ProductCommandResponse pcr)
    {
        
    }

    private ProductEvent ToProductEvent(IProductEvent e)
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
        }
    }

    private Proto.ProductSold ToProto(ProductSold changed)
    {
        
    }

    private static InventoryChanged ToProto(ProductInventoryChanged changed)
    {
        var inventoryChanged = new Proto.InventoryChanged();
        var (productId, quantity, inventoryChangeReason) = changed;
        inventoryChanged.Reason = ToProto(inventoryChangeReason);
        inventoryChanged.ProductId = productId;
        inventoryChanged.QuantityChanged = quantity;
        return inventoryChanged;
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

    private static Proto.ProductCreated ToProto(ProductCreated created)
    {
        var productCreated = new  Proto.ProductCreated
        {
            Data = CreateProductData(created)
        };
        return productCreated;
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
}

public static class DecimalSerializationExtensions
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
        return new DecimalValue(){ Units=units, Nanos = nanos};
    }
}