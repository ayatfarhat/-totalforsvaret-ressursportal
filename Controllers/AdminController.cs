using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.Services;

namespace Nabohjelp.Controllers;

// Administrasjon av brukerkontoer. Bare for rollen Admin (se policyen "Admin" i Program.cs).
//   GET  /Admin                  -> liste over alle brukere
//   POST /Admin/EndreRolle       -> gi en bruker en annen rolle
//   POST /Admin/Laas             -> låse en konto (brukeren kan ikke logge inn)
//   POST /Admin/LaasOpp          -> låse opp en konto
//   POST /Admin/NullstillTofa    -> slå av 2FA for en bruker som har mistet telefonen
//   POST /Admin/SlettBruker      -> slette en bruker og registreringene hens
[Authorize(Policy = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly KontoService _kontoService;
    private readonly IStringLocalizer<SharedResource> T;

    public AdminController(
        UserManager<IdentityUser> userManager,
        KontoService kontoService,
        IStringLocalizer<SharedResource> localizer)
    {
        _userManager = userManager;
        _kontoService = kontoService;
        T = localizer;
    }

    // Viser alle brukere. Kan søke på e-post.
    [HttpGet]
    public async Task<IActionResult> Index(string? sok)
    {
        var brukere = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(sok))
        {
            brukere = brukere.Where(b => b.Email != null && b.Email.Contains(sok));
        }

        string? minId = _userManager.GetUserId(User);
        var liste = new List<AdminBrukerViewModel>();

        foreach (var bruker in brukere.OrderBy(b => b.Email).ToList())
        {
            var roller = await _userManager.GetRolesAsync(bruker);

            liste.Add(new AdminBrukerViewModel
            {
                Id = bruker.Id,
                Epost = bruker.Email ?? "",
                Rolle = roller.FirstOrDefault() ?? "",
                TofaErPa = bruker.TwoFactorEnabled,
                ErLaast = await _userManager.IsLockedOutAsync(bruker),
                ErMeg = bruker.Id == minId
            });
        }

        ViewData["Sok"] = sok;
        return View(liste);
    }

    // Gir brukeren en ny rolle. Hver bruker har én rolle.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndreRolle(string id, string rolle)
    {
        // Godtar bare rollene som finnes, i tilfelle noen har endret skjemaet.
        if (!RoleInitializer.Roller.Contains(rolle))
        {
            return BadRequest();
        }

        var bruker = await HentAnnenBruker(id);
        if (bruker == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var gamleRoller = await _userManager.GetRolesAsync(bruker);
        await _userManager.RemoveFromRolesAsync(bruker, gamleRoller);
        await _userManager.AddToRoleAsync(bruker, rolle);

        // Gjør at den nye rollen gjelder innen ett minutt, også hvis brukeren er logget inn nå.
        await _userManager.UpdateSecurityStampAsync(bruker);

        TempData["Melding"] = string.Format(T["{0} har nå rollen {1}."].Value, bruker.Email, rolle);
        return RedirectToAction(nameof(Index));
    }

    // Låser kontoen på ubestemt tid, for eksempel ved misbruk.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Laas(string id)
    {
        var bruker = await HentAnnenBruker(id);
        if (bruker == null)
        {
            return RedirectToAction(nameof(Index));
        }

        await _userManager.SetLockoutEnabledAsync(bruker, true);
        await _userManager.SetLockoutEndDateAsync(bruker, DateTimeOffset.MaxValue);

        // Logger brukeren ut innen ett minutt hvis hen er innlogget.
        await _userManager.UpdateSecurityStampAsync(bruker);

        TempData["Melding"] = string.Format(T["{0} er låst."].Value, bruker.Email);
        return RedirectToAction(nameof(Index));
    }

    // Låser opp kontoen og nullstiller antall feil passord.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LaasOpp(string id)
    {
        var bruker = await HentAnnenBruker(id);
        if (bruker == null)
        {
            return RedirectToAction(nameof(Index));
        }

        await _userManager.SetLockoutEndDateAsync(bruker, null);
        await _userManager.ResetAccessFailedCountAsync(bruker);

        TempData["Melding"] = string.Format(T["{0} er låst opp."].Value, bruker.Email);
        return RedirectToAction(nameof(Index));
    }

    // Slår av 2FA for en bruker som har mistet telefonen.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NullstillTofa(string id)
    {
        var bruker = await HentAnnenBruker(id);
        if (bruker == null)
        {
            return RedirectToAction(nameof(Index));
        }

        await _userManager.SetTwoFactorEnabledAsync(bruker, false);
        await _userManager.ResetAuthenticatorKeyAsync(bruker);

        TempData["Melding"] = string.Format(T["2FA er slått av for {0}."].Value, bruker.Email);
        return RedirectToAction(nameof(Index));
    }

    // Sletter brukeren og alle behov og ressurser hen har registrert.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SlettBruker(string id)
    {
        var bruker = await HentAnnenBruker(id);
        if (bruker == null)
        {
            return RedirectToAction(nameof(Index));
        }

        string epost = bruker.Email ?? "";
        string? feil = await _kontoService.SlettBrukerOgData(bruker);

        if (feil != null)
        {
            TempData["Feil"] = T[feil].Value;
        }
        else
        {
            TempData["Melding"] = string.Format(T["{0} og tilhørende registreringer er slettet."].Value, epost);
        }

        return RedirectToAction(nameof(Index));
    }

    // Finner brukeren, men stopper hvis admin prøver å endre sin egen konto.
    // Da kan ikke den siste administratoren låse seg selv ute ved et uhell.
    private async Task<IdentityUser?> HentAnnenBruker(string id)
    {
        if (id == _userManager.GetUserId(User))
        {
            TempData["Feil"] = T["Du kan ikke endre din egen konto her. Bruk Min konto."].Value;
            return null;
        }

        var bruker = await _userManager.FindByIdAsync(id);
        if (bruker == null)
        {
            TempData["Feil"] = T["Fant ikke brukeren."].Value;
        }

        return bruker;
    }
}
