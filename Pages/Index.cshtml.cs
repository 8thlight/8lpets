using Microsoft.AspNetCore.Mvc;

namespace _8lpets.Pages;

public class IndexModel : BasePageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        // No authentication required for the home page
    }
}
