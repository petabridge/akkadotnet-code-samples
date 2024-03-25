// -----------------------------------------------------------------------
//  <copyright file="ProductListing.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

namespace CqrsSqlServer.DataModel.Entities;

public class ProductListing
{
    public string ProductId { get; set; } = null!;
    
    public string ProductName { get; set; } = null!;
    
    public decimal Price { get; set; }
    
    public int RemainingInventory { get; set; }
    
    public int SoldUnits { get; set; }
    
    public decimal TotalRevenue { get; set; }
    
    public DateTime Created { get; set; }
    
    public DateTime LastModified { get; set; }
}