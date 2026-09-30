using Microsoft.AspNetCore.Mvc;
using Nabohjelp.Models;

namespace Nabohjelp.Controllers;

public class MapController : Controller
{
    // Viser kartet
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // Mottar koordinater
    [HttpPost]
    public IActionResult Index(MapViewModel model)
    {
        return View("Result", model);
    }
}