using Microsoft.AspNetCore.Mvc;
using Nabohjelp.ViewModels;

namespace Nabohjelp.Controllers;

// Controller for forsiden
public class HomeController : Controller
{
    // Viser forsiden
    public IActionResult Index()
    {
        // Lager en ViewModel med data til nettsiden
        var viewModel = new HomeViewModel
        {
            Title = "Nabohjelp",
            Message = "En plattform for deling av ressurser under kriser og nødsituasjoner."
        };

        // Sender ViewModel til View
        return View(viewModel);
    }
}