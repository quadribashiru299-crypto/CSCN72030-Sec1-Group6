using Microsoft.AspNetCore.Mvc;

namespace SmartShop.Hmi.Controllers;

public sealed class DashboardController(DashboardStateBridge bridge) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(bridge.GetSnapshot());

    [HttpGet]
    public IActionResult Snapshot() => Json(bridge.GetSnapshot());
}
