using System.Collections.Immutable;
using SqlSharding.Shared.Commands;
using SqlSharding.Shared.Events;

namespace SqlSharding.Shared;

public record ProductData(string ProductId, string ProductName, decimal CurrentPrice)
{
    public static readonly ProductData Empty = new(string.Empty, string.Empty, decimal.Zero);
}

public record PurchasingTotals(int RemainingInventory, int SoldInventory, decimal TotalRevenue)
{
    public static readonly PurchasingTotals Empty = new(0, 0, Decimal.Zero);
}

/// <summary>
/// The state object responsible for all event and message processing
/// </summary>
public record ProductState : IWithProductId
{
    public const int LowInventoryWarningThreshold = 3;
    
    public ProductData Data { get; init; } = ProductData.Empty;

    public string ProductId => Data.ProductId;

    public PurchasingTotals Totals { get; init; } = PurchasingTotals.Empty;

    public ImmutableSortedSet<ProductSold> Orders { get; init; } = ImmutableSortedSet<ProductSold>.Empty;
    
    public ImmutableSortedSet<ProductInventoryWarningEvent> Warnings { get; init; } = ImmutableSortedSet<ProductInventoryWarningEvent>.Empty;

    // TODO: could add product inventory change records here too

    public bool IsEmpty => Data == ProductData.Empty;


    /// <summary>
    /// Stateful processing of commands. Performs input validation et al.
    /// </summary>
    /// <remarks>
    /// Intentionally kept simple.
    /// </remarks>
    /// <param name="productCommand">The command to process.</param>
    /// <returns></returns>
    public ProductCommandResponse ProcessCommand(IProductCommand productCommand)
    {
        switch (productCommand)
        {
            case CreateProduct create when IsEmpty:
            {
                if (IsEmpty)
                {
                    // not initialized, can create

                    // events
                    var productCreated = new ProductCreated(create.ProductId, create.ProductName, create.Price);
                    var productInventoryChanged = new ProductInventoryChanged(create.ProductId, create.InitialQuantity,
                        InventoryChangeReason.SupplyIncrease);


                    var response = new ProductCommandResponse(create.ProductId,
                        new IProductEvent[] { productCreated, productInventoryChanged });
                    return response;
                }
                else
                {
                    return new ProductCommandResponse(Data.ProductId, Array.Empty<IProductEvent>(), false,
                        $"Product with [Id={Data.ProductId}] already exists");
                }
            }
            case SupplyProduct(var productId, var additionalQuantity) when !IsEmpty:
            {
                var productInventoryChanged = new ProductInventoryChanged(productId, additionalQuantity,
                    InventoryChangeReason.SupplyIncrease);
                var response = new ProductCommandResponse(productId,
                    new IProductEvent[] { productInventoryChanged });
                return response;
            }
            // if Price is 0, then it is likely not set
            case PurchaseProduct purchase when !IsEmpty && Data.CurrentPrice > 0:
            {
                var events = new List<IProductEvent>();
                var productInventoryChanged = new ProductInventoryChanged(purchase.ProductId, purchase.NewOrder.Quantity,
                    InventoryChangeReason.Fulfillment);
                events.Add(productInventoryChanged);
                var backordered = false;

                if (Totals.RemainingInventory - purchase.NewOrder.Quantity <= LowInventoryWarningThreshold)
                {
                    if (Totals.RemainingInventory <= 0)
                    {
                        backordered = true;
                        var warningEvent = new ProductInventoryWarningEvent(purchase.ProductId,
                            ProductWarningReason.NoSupply, DateTime.UtcNow, $"Product [Id={Data.ProductId}, Name={Data.ProductName}] is now on backorder. [{Totals.RemainingInventory}] available inventory.");
                        events.Add(warningEvent);
                    }
                    else
                    {
                        var warningEvent = new ProductInventoryWarningEvent(purchase.ProductId,
                            ProductWarningReason.LowSupply, DateTime.UtcNow, $"Product [Id={Data.ProductId}, Name={Data.ProductName}] is low on supply. [{Totals.RemainingInventory}] available inventory.");
                        events.Add(warningEvent);
                    }
                }

                var productSold = new ProductSold(purchase.NewOrder, Data.CurrentPrice, backordered);
                events.Add(productSold);

                return new ProductCommandResponse(purchase.ProductId, events);
            }
            default:
            {
                return new ProductCommandResponse(productCommand.ProductId, Array.Empty<IProductEvent>(), false,
                    $"Product with [Id={Data.ProductId}] is not ready to process command [{productCommand}]");
            }
        }
    }

    public ProductState ProcessEvent(IProductEvent productEvent)
    {
        /*
         *     var newTotals = new PurchasingTotals(create.InitialQuantity, 0, decimal.Zero);
                    var newData = new ProductData(create.ProductId, create.ProductName, create.Price);
                    var newProduct = this with { Data = newData, Totals = newTotals };
         * 
         */
        switch (productEvent)
        {
            case ProductCreated(var productId, var productName, var price):
            {
                return this with
                {
                    Data = Data with
                    {
                        ProductId = productId, CurrentPrice = price,
                        ProductName = productName
                    }
                };
            }
            case ProductInventoryChanged(var productId, var quantity, var inventoryChangeReason):
            {
                return this with
                {
                    Totals = Totals with
                    {
                        RemainingInventory = Totals.RemainingInventory + quantity,
                        SoldInventory = inventoryChangeReason == InventoryChangeReason.Fulfillment
                            ? Totals.SoldInventory + quantity
                            : Totals.SoldInventory
                    }
                    
                    
                };
            }
            case ProductInventoryWarningEvent warning:
            {
                return this with
                {
                    Warnings = Warnings.Add(warning)
                };
            }
            case ProductSold sold:
            {
                return this with
                {
                    Orders = Orders.Add(sold),
                    Totals = Totals with
                    {
                        TotalRevenue = Totals.TotalRevenue + sold.TotalPrice
                    }
                };
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(productEvent));
        }
    }
}