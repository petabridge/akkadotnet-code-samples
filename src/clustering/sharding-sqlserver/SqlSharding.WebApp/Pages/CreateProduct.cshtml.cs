using System.ComponentModel.DataAnnotations;
using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlSharding.Shared.Commands;
using SqlSharding.Shared.Sharding;

namespace SqlSharding.WebApp.Pages;


public sealed class NewProduct
{
    [BindProperty]
    [Required]
    [MinLength(6)]
    [MaxLength(50)]
    public string ProductName { get; set; }
    
    [Range(1, 100000)]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }
    
    [Range(0, 100000)]
    public int InitialQuantity { get; set; }

    public string? Tags { get; set; } = string.Empty;
}

public class CreateProduct : PageModel
{
    public CreateProduct(ActorRegistry registry)
    {
        _productActor = registry.Get<ProductMarker>();
    }

    [BindProperty]
    public NewProduct Product { get; set; }
    
    private readonly IActorRef _productActor;
    
    public void OnGet()
    {
        
    }
    
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var createProductCommand = new SqlSharding.Shared.Commands.CreateProduct(Guid.NewGuid().ToString(),
            Product.ProductName, Product.Price, Product.InitialQuantity, Product.Tags?.Split(';') ?? Array.Empty<string>());

        var createRsp = await _productActor.Ask<ProductCommandResponse>(createProductCommand, TimeSpan.FromSeconds(3));

        return RedirectToPage("./product", new { productId = createProductCommand.ProductId });
    }
}