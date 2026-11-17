using Microsoft.AspNetCore.Mvc;
using SmartShop.Services;

namespace SmartShop.Controllers;

public class HomeController : Controller
{
    private readonly MockDataService _data;

    public HomeController(MockDataService data) => _data = data;

    public IActionResult Index() => View();

    /// <summary>Staff HMI dashboard: one simulation time step per request.</summary>
    public IActionResult Staff() => View(_data.GetNextBatch());

    /// <summary>Read-only customer view of the same telemetry stream.</summary>
    public IActionResult Customer() => View(_data.GetNextBatch());

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error() => View();
}
