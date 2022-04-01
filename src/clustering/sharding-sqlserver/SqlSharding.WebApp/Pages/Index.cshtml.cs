using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlSharding.Shared.Queries;
using SqlSharding.Shared.Sharding;

namespace SqlSharding.WebApp.Pages;
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IActorRef _indexActor;

    public IndexModel(ILogger<IndexModel> logger, ActorRegistry registry)
    {
        _logger = logger;
        _indexActor = registry.Get<ProductIndexMarker>();
    }

    public IReadOnlyList<string> ProductIds { get; set; } = Array.Empty<string>();

    public async Task<IActionResult> OnGetAsync()
    {
        var products = await _indexActor.Ask<FetchAllProductsResponse>(FetchAllProducts.Instance, TimeSpan.FromSeconds(5));
        
        ProductIds = products.ProductIds;
        
        return Page();
    }
}