﻿using System.Collections.Immutable;

namespace SqlSharding.Shared.Queries;

/// <summary>
/// Query to the index actor to retrieve all products
/// </summary>
public sealed class FetchAllProducts
{
    public static readonly FetchAllProducts Instance = new();
    private FetchAllProducts(){}
}

public sealed class FetchAllProductsResponse
{
    public FetchAllProductsResponse(IReadOnlyList<string> productIds)
    {
        ProductIds = productIds;
    }

    public IReadOnlyList<string> ProductIds { get; }
}