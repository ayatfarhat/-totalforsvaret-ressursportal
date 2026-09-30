using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nabohjelp.ViewModels;

namespace Nabohjelp.Controllers;

// Controller for ressurser
// [Authorize]
public class ResourceController : Controller
{
    // Viser skjemaet for å opprette en ressurs
    [HttpGet]
    public IActionResult Create()
    {
        // Lager en tom ViewModel og sender den til View
        return View(new ResourceViewModel());
    }
}