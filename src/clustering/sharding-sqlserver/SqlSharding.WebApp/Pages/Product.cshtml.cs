using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlSharding.Shared;
using SqlSharding.Shared.Queries;
using SqlSharding.Shared.Sharding;

namespace SqlSharding.WebApp.Pages;

public class Product : PageModel
{
    private readonly IActorRef _productActor;

    public Product(ActorRegistry registry)
    {
        _productActor = registry.Get<ProductMarker>();
    }

    [BindProperty(SupportsGet = true)]
    public string ProductId { get; set; }
    
    public ProductState State { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var result = await _productActor.Ask<FetchResult>(new FetchProduct(ProductId), TimeSpan.FromSeconds(3));
        State = result.State;

        if (State.IsEmpty) // no product with this id
            return NotFound();

        return Page();
    }
}