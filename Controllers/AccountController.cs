using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Nabohjelp.Models;

namespace Nabohjelp.Controllers;

// Håndterer registrering, innlogging, utlogging og tofaktor (2FA).
// Vi bruker ASP.NET Core Identity:
//   UserManager   = lage og finne brukere, roller og 2FA-nøkler
//   SignInManager = logge inn og ut
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ---------- REGISTRERING ----------

    // GET: /Account/Register  -> viser skjemaet
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Account/Register  -> lager ny bruker
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Sjekker om e-posten er i bruk fra før
        var finnes = await _userManager.FindByEmailAsync(model.Epost);
        if (finnes != null)
        {
            ModelState.AddModelError("Epost", "E-posten er allerede registrert");
            return View(model);
        }

        // Lager brukeren. Identity lagrer passordet kryptert (hash), aldri i klartekst.
        var bruker = new IdentityUser { UserName = model.Epost, Email = model.Epost };
        var resultat = await _userManager.CreateAsync(bruker, model.Passord);
        if (!resultat.Succeeded)
        {
            // Identity har egne passordregler. Vi oversetter feilene til norsk,
            // slik at brukeren får vite hva som mangler i stedet for en generell melding.
            foreach (var feil in resultat.Errors)
            {
                ModelState.AddModelError("", OversettFeil(feil.Code));
            }
            return View(model);
        }

        // Alle nye brukere får rollen Frivillig
        await _userManager.AddToRoleAsync(bruker, "Frivillig");

        // Logger inn brukeren med en gang
        await _signInManager.SignInAsync(bruker, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    // ---------- INNLOGGING ----------

    // GET: /Account/Login  -> viser skjemaet
    // returnUrl er siden brukeren prøvde å åpne før hen ble sendt til innlogging.
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Login  -> sjekker e-post og passord
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // lockoutOnFailure: true -> kontoen låses etter 5 feil (se Program.cs)
        var resultat = await _signInManager.PasswordSignInAsync(
            model.Epost, model.Passord, isPersistent: false, lockoutOnFailure: true);

        if (resultat.Succeeded)
        {
            return TilbakeEllerForside(returnUrl);
        }

        if (resultat.RequiresTwoFactor)
        {
            // Riktig passord, men brukeren har 2FA og må skrive inn kode
            return RedirectToAction("LoginWith2fa", new { returnUrl });
        }

        if (resultat.IsLockedOut)
        {
            ModelState.AddModelError("", "Kontoen er låst i 5 minutter etter for mange feil.");
            return View(model);
        }

        // Vi sier ikke om det var e-posten eller passordet som var feil (sikkerhet)
        ModelState.AddModelError("", "Feil e-post eller passord");
        return View(model);
    }

    // GET: /Account/LoginWith2fa  -> ber om koden fra appen
    [HttpGet]
    public IActionResult LoginWith2fa(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/LoginWith2fa  -> sjekker koden
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(TofaViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultat = await _signInManager.TwoFactorAuthenticatorSignInAsync(
            model.Kode, isPersistent: false, rememberClient: false);

        if (resultat.Succeeded)
        {
            return TilbakeEllerForside(returnUrl);
        }

        ModelState.AddModelError("Kode", "Feil kode");
        return View(model);
    }

    // Sender brukeren tilbake til siden hen kom fra. Vi tillater bare adresser på
    // vår egen side (IsLocalUrl), slik at ingen kan lage en lenke som sender
    // brukeren videre til en falsk nettside etter innlogging.
    private IActionResult TilbakeEllerForside(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    // Gjør feilkodene fra Identity om til norsk tekst.
    private static string OversettFeil(string kode)
    {
        switch (kode)
        {
            case "PasswordTooShort":
                return "Passordet er for kort. Det må ha minst 8 tegn.";
            case "PasswordRequiresDigit":
                return "Passordet må ha minst ett tall.";
            case "PasswordRequiresLower":
                return "Passordet må ha minst én liten bokstav.";
            case "PasswordRequiresUpper":
                return "Passordet må ha minst én stor bokstav.";
            case "PasswordRequiresNonAlphanumeric":
                return "Passordet må ha minst ett spesialtegn, for eksempel !";
            case "DuplicateUserName":
            case "DuplicateEmail":
                return "E-posten er allerede registrert";
            default:
                return "Kunne ikke lage brukeren. Prøv igjen.";
        }
    }

    // ---------- UTLOGGING ----------

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // Vises når brukeren er innlogget, men ikke har riktig rolle
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ---------- MIN KONTO OG 2FA ----------

    // GET: /Account  -> viser e-post, rolle og om 2FA er på
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var bruker = await _userManager.GetUserAsync(User);
        if (bruker == null)
        {
            return RedirectToAction("Login");
        }
        var roller = await _userManager.GetRolesAsync(bruker);

        var model = new KontoViewModel
        {
            Epost = bruker.Email ?? "",
            Rolle = string.Join(", ", roller),
            TofaErPa = bruker.TwoFactorEnabled
        };
        return View(model);
    }

    // GET: /Account/AktiverTofa  -> viser nøkkel og QR-kode
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> AktiverTofa()
    {
        var bruker = await _userManager.GetUserAsync(User);
        if (bruker == null)
        {
            return RedirectToAction("Login");
        }

        // Henter brukerens hemmelige nøkkel. Lager en ny hvis den ikke finnes.
        var nokkel = await _userManager.GetAuthenticatorKeyAsync(bruker);
        if (nokkel == null)
        {
            await _userManager.ResetAuthenticatorKeyAsync(bruker);
            nokkel = await _userManager.GetAuthenticatorKeyAsync(bruker);
        }

        var model = new TofaViewModel { Nokkel = nokkel ?? "" };
        return View(model);
    }

    // POST: /Account/AktiverTofa  -> sjekker koden og slår på 2FA
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AktiverTofa(TofaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var bruker = await _userManager.GetUserAsync(User);
        if (bruker == null)
        {
            return RedirectToAction("Login");
        }

        // "Authenticator" betyr at koden kommer fra en autentiseringsapp
        var riktigKode = await _userManager.VerifyTwoFactorTokenAsync(bruker, "Authenticator", model.Kode);
        if (!riktigKode)
        {
            ModelState.AddModelError("Kode", "Feil kode, prøv igjen");
            return View(model);
        }

        await _userManager.SetTwoFactorEnabledAsync(bruker, true);
        TempData["Melding"] = "2FA er slått på!";
        return RedirectToAction("Index");
    }
}
