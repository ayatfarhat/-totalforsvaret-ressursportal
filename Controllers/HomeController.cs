using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Nabohjelp.Models;
using Nabohjelp.Services;
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
        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
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

    // Vilkår for bruk: reglene for å bruke tjenesten.
    [HttpGet]
    public IActionResult Vilkar()
    {
        return View();
    }

    // Hvilke cookies siden bruker, og hvorfor.
    [HttpGet]
    public IActionResult Cookies()
    {
        return View();
    }

    // Spørsmål og svar. Spørsmålene står i Models/VanligeSporsmal.cs.
    [HttpGet]
    public IActionResult Sporsmal()
    {
        return View(VanligeSporsmal.Alle);
    }

    // «OK» på cookie-informasjonen nederst på siden (Views/Shared/_CookieInfo.cshtml).
    // Vi bruker bare nødvendige cookies, så det er ingenting å godta eller avslå.
    // Cookien sørger bare for at informasjonen ikke vises igjen.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LestCookieInfo(string? returnUrl)
    {
        Response.Cookies.Append(Nettsted.CookieInfoNavn, "lest", new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax
        });

        return TilbakeTil(returnUrl);
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

        return TilbakeTil(returnUrl);
    }

    // Sender brukeren tilbake til siden hen var på. Bare adresser på vår egen side,
    // så lenken ikke kan sende brukeren videre til en annen nettside.
    private IActionResult TilbakeTil(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
