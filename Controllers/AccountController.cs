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

    // Gir tilgang til Identity og databasen.
    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
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
            ModelState.AddModelError(
                "",
                "Kunne ikke lage brukeren. Prøv igjen.");

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

    // Viser innloggingsskjemaet.
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // Kontrollerer e-post og passord.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
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
            return RedirectToAction("Index", "Home");
        }

        // Ber om kode hvis tofaktor er aktivert.
        if (resultat.RequiresTwoFactor)
        {
            return RedirectToAction(nameof(LoginWith2fa));
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
    public IActionResult LoginWith2fa()
    {
        return View();
    }

    // Kontrollerer koden fra autentiseringsappen.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(TofaViewModel model)
    {
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
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("Kode", "Feil kode");
        return View(model);
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

        // Logger ut etter fullført sletting.
        await _signInManager.SignOutAsync();

        TempData["Melding"] =
            "Kontoen din og tilhørende registreringer er slettet.";

        return RedirectToAction("Index", "Home");
    }
}