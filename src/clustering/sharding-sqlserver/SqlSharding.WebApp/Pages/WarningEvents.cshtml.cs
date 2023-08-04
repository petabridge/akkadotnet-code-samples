using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SqlSharding.Shared;
using SqlSharding.WebApp.Services;

namespace SqlSharding.WebApp.Pages;

public class WarningEvents : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IProductsResolver _resolver;

    public WarningEvents(ILogger<IndexModel> logger, IProductsResolver resolver)
    {
        _logger = logger;
        _resolver = resolver;
    }

    public IReadOnlyList<WarningEventData> Warnings { get; set; } = ImmutableList<WarningEventData>.Empty;
    
    public async Task<IActionResult> OnGetAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var warnings = await _resolver.FetchWarningEventsAsync(cts.Token);

        Warnings = warnings.Warnings.Where(p => p.Warnings.Count > 0).OrderBy(p => p).ToList();
        
        return Page();
    }
}