using System.Collections.Immutable;
using Akka.Actor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlSharding.Shared;
using SqlSharding.WebApp.Services;

namespace SqlSharding.WebApp.Pages;

public class Sold : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IProductsResolver _resolver;

    public Sold(ILogger<IndexModel> logger, IProductsResolver resolver)
    {
        _logger = logger;
        _resolver = resolver;
    }

    public IReadOnlyList<ProductsSoldData> Products { get; set; } = ImmutableList<ProductsSoldData>.Empty;
    
    public async Task<IActionResult> OnGetAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var products = await _resolver.FetchSoldProductsAsync(cts.Token);

        Products = products.Products.Where(p => p.Invoices.Count > 0).OrderBy(p => p).ToList();
        
        return Page();
    }
}