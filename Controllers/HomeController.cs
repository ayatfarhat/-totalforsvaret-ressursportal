using System.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;

namespace Nabohjelp.Controllers;

// Controller for forsiden
public class HomeController : Controller
{
    // Viser forsiden. All tekst står i Views/Home/Index.cshtml.
    public IActionResult Index()
    {
        return View();
    }

    // Feilsiden. Brukes av UseExceptionHandler("/Home/Error") i Program.cs
    // når noe uventet går galt i produksjon.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        // Koden som vises på siden, så feilen kan finnes igjen i loggen.
        string requestId = HttpContext.TraceIdentifier;
        if (Activity.Current != null && Activity.Current.Id != null)
        {
            requestId = Activity.Current.Id;
        }

        return View(new ErrorViewModel { RequestId = requestId });
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

    // Personvernerklæringen (GDPR): hva vi lagrer, hvorfor, hvor lenge og hvilke rettigheter brukeren har.
    [HttpGet]
    public IActionResult Personvern()
    {
        return View();
    }

    // Bytter språk mellom norsk ("nb") og engelsk ("en").
    // Valget lagres i en cookie i ett år, og brukeren sendes tilbake til samme side.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult VelgSprak(string sprak, string? returnUrl)
    {
        if (sprak != "nb" && sprak != "en")
        {
            sprak = "nb";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(sprak)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

        // Bare adresser på vår egen side, så lenken ikke kan sende brukeren videre til en annen nettside.
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
