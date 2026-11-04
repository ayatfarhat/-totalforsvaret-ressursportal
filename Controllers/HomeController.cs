using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Nabohjelp.Models;
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
            Title = "BeredskapsPortalen",
            Message = "En plattform for deling av ressurser under kriser og nødsituasjoner."
        };

        // Sender ViewModel til View
        return View(viewModel);
    }

    // Feilsiden. Brukes av UseExceptionHandler("/Home/Error") i Program.cs
    // når noe uventet går galt i produksjon.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    // Side for feilkoder (for eksempel 404). Brukes av
    // UseStatusCodePagesWithReExecute("/Home/Status/{0}") i Program.cs.
    [Route("Home/Status/{kode:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int kode)
    {
        ViewBag.Kode = kode;
        return View();
    }
}
