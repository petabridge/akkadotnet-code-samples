﻿using System.Collections.Immutable;
using SqlSharding.Shared.Commands;
using SqlSharding.Shared.Events;

namespace SqlSharding.Shared;

public record ProductData(string ProductId, string ProductName, decimal CurrentPrice) : ISqlShardingProtocolMember
{
    public static readonly ProductData Empty = new(string.Empty, string.Empty, decimal.Zero);
}

public record PurchasingTotals(int RemainingInventory, int SoldInventory, decimal TotalRevenue) : ISqlShardingProtocolMember
{
    public static readonly PurchasingTotals Empty = new(0, 0, decimal.Zero);
}

public static class ProductStateExtensions
{
    /// <summary>
    /// Stateful processing of commands. Performs input validation et al.
    /// </summary>
    /// <remarks>
    /// Intentionally kept simple.
    /// </remarks>
    /// <param name="productState">The product state we're evaluating.</param>
    /// <param name="productCommand">The command to process.</param>
    /// <returns></returns>
    public static ProductCommandResponse ProcessCommand(this ProductState productState, IProductCommand productCommand)
    {
        switch (productCommand)
        {
            case CreateProduct create when productState.IsEmpty:
            {
                if (productState.IsEmpty)
                {
                    // not initialized, can create

                    // events
                    var productCreated = new ProductCreated(create.ProductId, create.ProductName, create.Price);
                    var productInventoryChanged = new ProductInventoryChanged(create.ProductId, create.InitialQuantity,
                        DateTime.UtcNow,
                        InventoryChangeReason.SupplyIncrease);


                    var response = new ProductCommandResponse(create.ProductId,
                        new IProductEvent[] { productCreated, productInventoryChanged });
                    return response;
                }
                else
                {
                    return new ProductCommandResponse(productState.Data.ProductId, Array.Empty<IProductEvent>(), false,
                        $"Product with [Id={productState.Data.ProductId}] already exists");
                }
            }
            case SupplyProduct(var productId, var additionalQuantity) when !productState.IsEmpty:
            {
                var productInventoryChanged = new ProductInventoryChanged(productId, additionalQuantity, DateTime.UtcNow,
                    InventoryChangeReason.SupplyIncrease);
                var response = new ProductCommandResponse(productId,
                    new IProductEvent[] { productInventoryChanged });
                return response;
            }
            // if Price is 0, then it is likely not set
            case PurchaseProduct purchase when !productState.IsEmpty && productState.Data.CurrentPrice > 0:
            {
                var events = new List<IProductEvent>();
                var productInventoryChanged = new ProductInventoryChanged(purchase.ProductId, -1*purchase.NewOrder.Quantity,
                    DateTime.UtcNow,
                    InventoryChangeReason.Fulfillment);
                events.Add(productInventoryChanged);
                var backordered = false;

                if (productState.Totals.RemainingInventory - purchase.NewOrder.Quantity <= ProductState.LowInventoryWarningThreshold)
                {
                    if (productState.Totals.RemainingInventory <= 0)
                    {
                        backordered = true;
                        var warningEvent = new ProductInventoryWarningEvent(purchase.ProductId,
                            ProductWarningReason.NoSupply, DateTime.UtcNow, $"Product [Id={productState.Data.ProductId}, Name={productState.Data.ProductName}] is now on backorder. [{productState.Totals.RemainingInventory}] available inventory.");
                        events.Add(warningEvent);
                    }
                    else
                    {
                        var warningEvent = new ProductInventoryWarningEvent(purchase.ProductId,
                            ProductWarningReason.LowSupply, DateTime.UtcNow, $"Product [Id={productState.Data.ProductId}, Name={productState.Data.ProductName}] is low on supply. [{productState.Totals.RemainingInventory}] available inventory.");
                        events.Add(warningEvent);
                    }
                }

                var productSold = new ProductSold(purchase.NewOrder, productState.Data.CurrentPrice, backordered);
                events.Add(productSold);

                return new ProductCommandResponse(purchase.ProductId, events);
            }
            default:
            {
                return new ProductCommandResponse(productCommand.ProductId, Array.Empty<IProductEvent>(), false,
                    $"Product with [Id={productState.Data.ProductId}] is not ready to process command [{productCommand}]");
            }
        }
    }

    public static ProductState ProcessEvent(this ProductState productState, IProductEvent productEvent)
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
                return productState with
                {
                    Data = productState.Data with
                    {
                        ProductId = productId, CurrentPrice = price,
                        ProductName = productName
                    }
                };
            }
            case ProductInventoryChanged(var productId, var quantity, var timestamp, var inventoryChangeReason) @event:
            {
                return productState with
                {
                    Totals = productState.Totals with
                    {
                        RemainingInventory = productState.Totals.RemainingInventory + quantity,
                        SoldInventory = inventoryChangeReason == InventoryChangeReason.Fulfillment
                            ? productState.Totals.SoldInventory + Math.Abs(quantity)
                            : productState.Totals.SoldInventory
                    },
                    InventoryChanges = productState.InventoryChanges.Add(@event)
                };
            }
            case ProductInventoryWarningEvent warning:
            {
                return productState with
                {
                    Warnings = productState.Warnings.Add(warning)
                };
            }
            case ProductSold sold:
            {
                return productState with
                {
                    Orders = productState.Orders.Add(sold),
                    Totals = productState.Totals with
                    {
                        TotalRevenue = productState.Totals.TotalRevenue + sold.TotalPrice
                    }
                };
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(productEvent));
        }
    }
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
    public ImmutableSortedSet<ProductInventoryChanged> InventoryChanges { get; init; } = ImmutableSortedSet<ProductInventoryChanged>.Empty;

    public bool IsEmpty => Data == ProductData.Empty;
}