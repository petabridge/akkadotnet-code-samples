using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlSharding.Shared;
using SqlSharding.Shared.Queries;
using SqlSharding.Shared.Sharding;
using SqlSharding.WebApp.Services;

namespace SqlSharding.WebApp.Pages;
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IProductsResolver _resolver;

    public IndexModel(ILogger<IndexModel> logger, IProductsResolver resolver)
    {
        _logger = logger;
        _resolver = resolver;
    }

    public IReadOnlyList<ProductData> Products { get; set; } = Array.Empty<ProductData>();

    public async Task<IActionResult> OnGetAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var products = await _resolver.FetchAllProductsAsync(cts.Token);
        
        Products = products.Products.OrderByDescending(c => c.CurrentPrice).ToList();
        
        return Page();
    }
}