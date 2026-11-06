using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Nabohjelp.Models;
using Nabohjelp.Services;

namespace Nabohjelp.Controllers;

// Håndterer brukerkontoer, innlogging, tofaktor og personvern (GDPR).
//
// Det meste av arbeidet gjøres av ASP.NET Core Identity:
//   UserManager   – lager, finner, endrer og sletter brukere (passord, roller, 2FA-nøkkel).
//   SignInManager – logger brukere inn og ut (lager og fjerner innloggingskaken).
// Controlleren sjekker skjemaene, kaller Identity og viser riktig side eller feilmelding.
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly KontoService _kontoService;
    private readonly IStringLocalizer<SharedResource> T;

    // Gir tilgang til Identity, kontotjenesten (sletting og nedlasting av data)
    // og oversettelsene (T["..."]).
    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        KontoService kontoService,
        IStringLocalizer<SharedResource> localizer)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _kontoService = kontoService;
        T = localizer;
    }

    // ---------- REGISTRERING ----------

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Account/Register
    // Oppretter en ny bruker med rollen Frivillig og logger hen inn.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // GDPR: brukeren må ha lest hvordan opplysningene brukes før kontoen lages.
        if (!model.GodtarPersonvern)
        {
            ModelState.AddModelError(nameof(model.GodtarPersonvern),
                T["Du må godta personvernerklæringen for å lage en konto."]);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Kontrollerer om e-posten allerede er registrert.
        var finnes = await _userManager.FindByEmailAsync(model.Epost);

        if (finnes != null)
        {
            ModelState.AddModelError("Epost", T["E-posten er allerede registrert"]);
            return View(model);
        }

        var bruker = new IdentityUser
        {
            UserName = model.Epost,
            Email = model.Epost
        };

        // Identity lagrer bare en hash av passordet, aldri selve passordet.
        var resultat = await _userManager.CreateAsync(bruker, model.Passord);

        if (!resultat.Succeeded)
        {
            // Identity har egne passordregler. Vi oversetter feilene,
            // slik at brukeren får vite hva som mangler i stedet for en generell melding.
            foreach (var feil in resultat.Errors)
            {
                ModelState.AddModelError("", T[OversettFeil(feil.Code)]);
            }
            return View(model);
        }

        // Alle nye brukere får rollen Frivillig.
        await _userManager.AddToRoleAsync(bruker, "Frivillig");

        await _signInManager.SignInAsync(bruker, isPersistent: false);

        return RedirectToAction("Index", "Home");
    }

    // ---------- INNLOGGING ----------

    // GET: /Account/Login
    // returnUrl er siden brukeren prøvde å åpne før hen ble sendt til innlogging.
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Login
    // Kontrollerer e-post og passord.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // isPersistent: false   – brukeren logges ut når nettleseren lukkes.
        // lockoutOnFailure: true – kontoen låses en stund etter flere feil passord.
        var resultat = await _signInManager.PasswordSignInAsync(
            model.Epost,
            model.Passord,
            isPersistent: false,
            lockoutOnFailure: true);

        if (resultat.Succeeded)
        {
            return TilbakeEllerForside(returnUrl);
        }

        // Riktig passord, men brukeren har slått på 2FA og må skrive inn koden fra appen.
        if (resultat.RequiresTwoFactor)
        {
            return RedirectToAction("LoginWith2fa", new { returnUrl });
        }

        // Låst etter for mange feil passord, eller låst av administrator.
        if (resultat.IsLockedOut)
        {
            ModelState.AddModelError("", T["Kontoen er låst. Prøv igjen senere, eller kontakt administrator."]);
            return View(model);
        }

        // Samme melding om e-posten eller passordet er feil, så ingen kan finne ut hvem som har konto.
        ModelState.AddModelError("", T["Feil e-post eller passord"]);
        return View(model);
    }

    // GET: /Account/LoginWith2fa
    // Viser skjemaet for koden fra autentiseringsappen.
    [HttpGet]
    public IActionResult LoginWith2fa(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/LoginWith2fa
    // Kontrollerer koden fra autentiseringsappen. Identity husker fra forrige steg
    // hvem som skrev riktig passord, så her trengs bare koden.
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
            model.Kode,
            isPersistent: false,
            rememberClient: false);

        if (resultat.Succeeded)
        {
            return TilbakeEllerForside(returnUrl);
        }

        if (resultat.IsLockedOut)
        {
            ModelState.AddModelError("", T["Kontoen er låst. Prøv igjen senere, eller kontakt administrator."]);
            return View(model);
        }

        ModelState.AddModelError("Kode", T["Feil kode"]);
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

    // Identity gir feil som engelske koder (for eksempel «PasswordTooShort»).
    // Her blir de til norsk tekst. T[...] oversetter videre hvis engelsk er valgt.
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
            case "PasswordMismatch":
                return "Feil passord.";
            case "DuplicateUserName":
            case "DuplicateEmail":
                return "E-posten er allerede registrert";
            default:
                return "Kunne ikke lagre. Prøv igjen.";
        }
    }

    // ---------- UTLOGGING ----------

    // POST: /Account/Logout
    // Utlogging er POST (ikke en vanlig lenke), så en annen nettside ikke kan logge brukeren ut.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/AccessDenied
    // Hit sendes brukeren når hen er logget inn, men mangler tilgang
    // (feil rolle, eller Kommune/Admin uten 2FA når det kreves).
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ---------- MIN KONTO ----------

    // GET: /Account
    // Viser e-post, roller og tofaktorstatus.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Henter brukeren fra databasen ut fra innloggingskaken.
        // Den kan være null hvis kontoen er slettet mens brukeren fortsatt var innlogget.
        // Samme sjekk står i alle handlingene under.
        var bruker = await _userManager.GetUserAsync(User);

        if (bruker == null)
        {
            return RedirectToAction(nameof(Login));
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

    // ---------- ENDRE PASSORD ----------

    // GET: /Account/EndrePassord
    [Authorize]
    [HttpGet]
    public IActionResult EndrePassord()
    {
        return View(new EndrePassordViewModel());
    }

    // POST: /Account/EndrePassord
    // Bytter passord. Krever det gamle passordet, så ingen kan bytte det fra en åpen PC.
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndrePassord(EndrePassordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var bruker = await _userManager.GetUserAsync(User);

        if (bruker == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var resultat = await _userManager.ChangePasswordAsync(bruker, model.NaavarendePassord, model.NyttPassord);

        if (!resultat.Succeeded)
        {
            foreach (var feil in resultat.Errors)
            {
                ModelState.AddModelError("", T[OversettFeil(feil.Code)]);
            }
            return View(model);
        }

        // Når passordet byttes, blir brukeren etter hvert logget ut på andre enheter.
        // Her på denne enheten fornyer vi innloggingen, så brukeren slipper å logge inn igjen.
        await _signInManager.RefreshSignInAsync(bruker);

        TempData["Melding"] = T["Passordet er endret."].Value;
        return RedirectToAction(nameof(Index));
    }

    // ---------- TOFAKTOR ----------

    // GET: /Account/AktiverTofa
    // Viser nøkkelen (og QR-koden) som brukeren legger inn i autentiseringsappen.
    // Appen bruker nøkkelen til å lage en ny sekssifret kode hvert 30. sekund.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> AktiverTofa()
    {
        var bruker = await _userManager.GetUserAsync(User);

        if (bruker == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var nokkel = await _userManager.GetAuthenticatorKeyAsync(bruker);

        // Første gang finnes ingen nøkkel, så vi lager en.
        if (nokkel == null)
        {
            await _userManager.ResetAuthenticatorKeyAsync(bruker);
            nokkel = await _userManager.GetAuthenticatorKeyAsync(bruker);
        }

        var model = new TofaViewModel
        {
            Nokkel = nokkel ?? ""
        };

        return View(model);
    }

    // POST: /Account/AktiverTofa
    // Slår på 2FA bare hvis koden fra appen stemmer, så vi vet at appen er satt opp riktig.
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
            return RedirectToAction(nameof(Login));
        }

        var riktigKode = await _userManager.VerifyTwoFactorTokenAsync(bruker, "Authenticator", model.Kode);

        if (!riktigKode)
        {
            ModelState.AddModelError("Kode", T["Feil kode, prøv igjen"]);
            return View(model);
        }

        await _userManager.SetTwoFactorEnabledAsync(bruker, true);

        // Brukeren har nettopp vist en riktig kode, så vi logger inn på nytt
        // og merker innloggingen som "med 2FA" (amr = mfa). Da slipper Admin og
        // Kommune å logge ut og inn igjen før de får tilgang til Kommandobroen.
        await _signInManager.SignInWithClaimsAsync(bruker, isPersistent: false,
            new[] { new Claim("amr", "mfa") });

        TempData["Melding"] = T["2FA er slått på!"].Value;

        return RedirectToAction(nameof(Index));
    }

    // POST: /Account/DeaktiverTofa
    // Slår av tofaktor og lager en ny nøkkel, så den gamle i appen ikke virker lenger.
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeaktiverTofa()
    {
        var bruker = await _userManager.GetUserAsync(User);

        if (bruker == null)
        {
            return RedirectToAction(nameof(Login));
        }

        await _userManager.SetTwoFactorEnabledAsync(bruker, false);
        await _userManager.ResetAuthenticatorKeyAsync(bruker);
        await _signInManager.RefreshSignInAsync(bruker);

        TempData["Melding"] = T["2FA er slått av."].Value;
        return RedirectToAction(nameof(Index));
    }

    // ---------- PERSONVERN (GDPR) ----------

    // GET: /Account/LastNedMineData
    // Laster ned alt vi har lagret om brukeren som en JSON-fil (rett til innsyn).
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> LastNedMineData()
    {
        var bruker = await _userManager.GetUserAsync(User);

        if (bruker == null)
        {
            return RedirectToAction(nameof(Login));
        }

        byte[] fil = await _kontoService.LagMineData(bruker);
        return File(fil, "application/json", "mine-data-beredskapsportalen.json");
    }

    // ---------- SLETTING AV KONTO ----------

    // GET: /Account/SlettKonto
    [Authorize]
    [HttpGet]
    public IActionResult SlettKonto()
    {
        return View(new SlettKontoViewModel());
    }

    // POST: /Account/SlettKonto
    // Sletter kontoen og brukerens behov og ressurser (rett til sletting).
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SlettKonto(SlettKontoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var bruker = await _userManager.GetUserAsync(User);

        if (bruker == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Krever riktig passord før sletting.
        bool riktigPassord = await _userManager.CheckPasswordAsync(bruker, model.Passord);

        if (!riktigPassord)
        {
            ModelState.AddModelError("Passord", T["Feil passord."]);
            return View(model);
        }

        // Selve slettingen skjer i KontoService (samme kode som admin bruker).
        string? feil = await _kontoService.SlettBrukerOgData(bruker);

        if (feil != null)
        {
            ModelState.AddModelError("", T[feil]);
            return View(model);
        }

        // Logger ut etter fullført sletting.
        await _signInManager.SignOutAsync();

        TempData["Melding"] = T["Kontoen din og tilhørende registreringer er slettet."].Value;

        return RedirectToAction("Index", "Home");
    }
}
