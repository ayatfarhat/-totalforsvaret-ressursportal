using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;

namespace Nabohjelp.Controllers;

// Håndterer brukerkontoer, innlogging og tofaktor.
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _miljo;

    // Gir tilgang til Identity, databasen og mappen med opplastede bilder.
    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext db,
        IWebHostEnvironment miljo)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _miljo = miljo;
    }

    // ---------- REGISTRERING ----------

    // Viser registreringsskjemaet.
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // Oppretter en ny bruker.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Kontrollerer om e-posten allerede er registrert.
        var finnes = await _userManager.FindByEmailAsync(model.Epost);

        if (finnes != null)
        {
            ModelState.AddModelError(
                "Epost",
                "E-posten er allerede registrert");

            return View(model);
        }

        var bruker = new IdentityUser
        {
            UserName = model.Epost,
            Email = model.Epost
        };

        // Identity lagrer en hash av passordet.
        var resultat = await _userManager.CreateAsync(
            bruker,
            model.Passord);

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

        // Alle nye brukere får rollen Frivillig.
        await _userManager.AddToRoleAsync(bruker, "Frivillig");

        await _signInManager.SignInAsync(
            bruker,
            isPersistent: false);

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

        var resultat = await _signInManager.PasswordSignInAsync(
            model.Epost,
            model.Passord,
            isPersistent: false,
            lockoutOnFailure: true);

        if (resultat.Succeeded)
        {
            return TilbakeEllerForside(returnUrl);
        }

        // Ber om kode hvis tofaktor er aktivert.
        if (resultat.RequiresTwoFactor)
        {
            // Riktig passord, men brukeren har 2FA og må skrive inn kode
            return RedirectToAction("LoginWith2fa", new { returnUrl });
        }

        if (resultat.IsLockedOut)
        {
            ModelState.AddModelError(
                "",
                "Kontoen er midlertidig låst etter for mange feil.");

            return View(model);
        }

        ModelState.AddModelError(
            "",
            "Feil e-post eller passord");

        return View(model);
    }

    // Viser skjemaet for tofaktorkode.
    [HttpGet]
    public IActionResult LoginWith2fa(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // Kontrollerer koden fra autentiseringsappen.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(TofaViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultat =
            await _signInManager.TwoFactorAuthenticatorSignInAsync(
                model.Kode,
                isPersistent: false,
                rememberClient: false);

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

    // Bildene ligger i wwwroot/uploads/ressurser og heter ressurs-ID + endelse (for eksempel 12.jpg).
    private void SlettRessursBilder(IEnumerable<int> ressursIder)
    {
        string mappe = Path.Combine(_miljo.WebRootPath, "uploads", "ressurser");
        foreach (int id in ressursIder)
        {
            foreach (string endelse in new[] { ".jpg", ".png", ".webp" })
            {
                string fil = Path.Combine(mappe, id + endelse);
                if (System.IO.File.Exists(fil))
                {
                    System.IO.File.Delete(fil);
                }
            }
        }
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

    // Logger ut brukeren.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction("Index", "Home");
    }

    // Vises når brukeren mangler tilgang.
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ---------- MIN KONTO ----------

    // Viser e-post, roller og tofaktorstatus.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
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

    // ---------- AKTIVERING AV TOFAKTOR ----------

    // Henter nøkkelen til autentiseringsappen.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> AktiverTofa()
    {
        var bruker = await _userManager.GetUserAsync(User);

        if (bruker == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var nokkel =
            await _userManager.GetAuthenticatorKeyAsync(bruker);

        if (nokkel == null)
        {
            await _userManager.ResetAuthenticatorKeyAsync(bruker);

            nokkel =
                await _userManager.GetAuthenticatorKeyAsync(bruker);
        }

        var model = new TofaViewModel
        {
            Nokkel = nokkel ?? ""
        };

        return View(model);
    }

    // Kontrollerer koden før tofaktor aktiveres.
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

        var riktigKode =
            await _userManager.VerifyTwoFactorTokenAsync(
                bruker,
                "Authenticator",
                model.Kode);

        if (!riktigKode)
        {
            ModelState.AddModelError(
                "Kode",
                "Feil kode, prøv igjen");

            return View(model);
        }

        await _userManager.SetTwoFactorEnabledAsync(bruker, true);

        TempData["Melding"] = "2FA er slått på!";

        return RedirectToAction(nameof(Index));
    }

    // ---------- SLETTING AV KONTO ----------

    // Viser skjemaet for kontosletting.
    [Authorize]
    [HttpGet]
    public IActionResult SlettKonto()
    {
        return View(new SlettKontoViewModel());
    }

    // Sletter kontoen og brukerens behov og ressurser.
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SlettKonto(
        SlettKontoViewModel model)
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
        bool riktigPassord =
            await _userManager.CheckPasswordAsync(
                bruker,
                model.Passord);

        if (!riktigPassord)
        {
            ModelState.AddModelError(
                "Passord",
                "Feil passord.");

            return View(model);
        }

        // Finner brukerens behov og ressurser.
        var behov = await _db.Behovsliste
            .Where(b => b.OpprettetAvBrukerId == bruker.Id)
            .ToListAsync();

        var ressurser = await _db.Ressurser
            .Where(r => r.OpprettetAvBrukerId == bruker.Id)
            .ToListAsync();

        var behovIder = behov.Select(b => b.Id).ToList();
        var ressursIder = ressurser.Select(r => r.Id).ToList();

        // Bevarer historikken hvis tildelinger er tilknyttet.
        bool harTildelinger = await _db.Tildelinger.AnyAsync(t =>
            t.TildeltAvBrukerId == bruker.Id ||
            behovIder.Contains(t.BehovId) ||
            ressursIder.Contains(t.RessursId));

        if (harTildelinger)
        {
            ModelState.AddModelError(
                "",
                "Kontoen har tilknyttede tildelinger og kan ikke " +
                "slettes automatisk. Kontakt administrator.");

            return View(model);
        }

        // Samler slettingen i én transaksjon.
        await using var transaksjon =
            await _db.Database.BeginTransactionAsync();

        try
        {
            // Sletter registreringene fra databasen.
            _db.Behovsliste.RemoveRange(behov);
            _db.Ressurser.RemoveRange(ressurser);

            await _db.SaveChangesAsync();

            // Identity sletter kontoen og rollekoblingene.
            var resultat = await _userManager.DeleteAsync(bruker);

            if (!resultat.Succeeded)
            {
                await transaksjon.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "Kunne ikke slette kontoen. Prøv igjen.");

                return View(model);
            }

            await transaksjon.CommitAsync();
        }
        catch (DbUpdateException)
        {
            // Angrer hvis databasen avviser slettingen.
            await transaksjon.RollbackAsync();

            ModelState.AddModelError(
                "",
                "Slettingen kunne ikke fullføres. Kontoen kan ha " +
                "fått nye tilknyttede registreringer. Prøv igjen " +
                "eller kontakt administrator.");

            return View(model);
        }

        // Sletter også bildene som hørte til ressursene, så ingenting
        // fra brukeren blir liggende igjen på serveren.
        SlettRessursBilder(ressursIder);

        // Logger ut etter fullført sletting.
        await _signInManager.SignOutAsync();

        TempData["Melding"] =
            "Kontoen din og tilhørende registreringer er slettet.";

        return RedirectToAction("Index", "Home");
    }
}