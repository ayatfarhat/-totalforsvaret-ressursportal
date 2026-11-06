using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using Nabohjelp.Services;

namespace Nabohjelp.Controllers;

// Administrasjon av brukerkontoer. Bare for rollen Admin (se policyen "Admin" i Program.cs).
//   GET  /Admin                  -> liste over alle brukere
//   POST /Admin/EndreRolle       -> gi en bruker en annen rolle
//   POST /Admin/Laas             -> låse en konto (brukeren kan ikke logge inn)
//   POST /Admin/LaasOpp          -> låse opp en konto
//   POST /Admin/NullstillTofa    -> slå av 2FA for en bruker som har mistet telefonen
//   POST /Admin/SlettBruker      -> slette en bruker og registreringene hens
//   GET  /Admin/Invitasjoner     -> invitasjonslenker som gir en rolle
//   POST /Admin/LagInvitasjon    -> lage en ny lenke
//   POST /Admin/SlettInvitasjon  -> trekke tilbake en lenke
[Authorize(Policy = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly KontoService _kontoService;
    private readonly ApplicationDbContext _db;
    private readonly IStringLocalizer<SharedResource> T;

    public AdminController(
        UserManager<IdentityUser> userManager,
        KontoService kontoService,
        ApplicationDbContext db,
        IStringLocalizer<SharedResource> localizer)
    {
        _userManager = userManager;
        _kontoService = kontoService;
        _db = db;
        T = localizer;
    }

    // Viser alle brukere. Kan søke på e-post.
    [HttpGet]
    public async Task<IActionResult> Index(string? sok)
    {
        // Spørringen mot databasen. b => ... betyr «for hver bruker b».
        IQueryable<IdentityUser> sporring = _userManager.Users;

        if (!string.IsNullOrWhiteSpace(sok))
        {
            sporring = sporring.Where(b => b.Email != null && b.Email.Contains(sok));
        }

        // Sortert på e-post.
        List<IdentityUser> brukere = sporring.OrderBy(b => b.Email).ToList();

        string? minId = _userManager.GetUserId(User);
        var liste = new List<AdminBrukerViewModel>();

        foreach (IdentityUser bruker in brukere)
        {
            // Hver bruker har én rolle (se EndreRolle).
            IList<string> roller = await _userManager.GetRolesAsync(bruker);
            string rolle = "";
            if (roller.Count > 0)
            {
                rolle = roller[0];
            }

            liste.Add(new AdminBrukerViewModel
            {
                Id = bruker.Id,
                Epost = bruker.Email,
                Rolle = rolle,
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

        IdentityUser? bruker = await HentAnnenBruker(id);
        if (bruker == null)
        {
            return RedirectToAction(nameof(Index));
        }

        IList<string> gamleRoller = await _userManager.GetRolesAsync(bruker);
        await _userManager.RemoveFromRolesAsync(bruker, gamleRoller);
        await _userManager.AddToRoleAsync(bruker, rolle);

        // Gjør at den nye rollen gjelder innen ett minutt, også hvis brukeren er logget inn nå.
        await _userManager.UpdateSecurityStampAsync(bruker);

        TempData["Melding"] = string.Format(T["{0} har nå rollen {1}."].Value, bruker.Email, T[rolle].Value);
        return RedirectToAction(nameof(Index));
    }

    // Låser kontoen på ubestemt tid, for eksempel ved misbruk.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Laas(string id)
    {
        IdentityUser? bruker = await HentAnnenBruker(id);
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
        IdentityUser? bruker = await HentAnnenBruker(id);
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
        IdentityUser? bruker = await HentAnnenBruker(id);
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
        IdentityUser? bruker = await HentAnnenBruker(id);
        if (bruker == null)
        {
            return RedirectToAction(nameof(Index));
        }

        string? epost = bruker.Email;
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

    // ---------- INVITASJONSLENKER ----------

    // Viser alle invitasjoner (nyeste først) og skjemaet for å lage en ny.
    [HttpGet]
    public async Task<IActionResult> Invitasjoner()
    {
        List<Invitasjon> liste = await _db.Invitasjoner
            .AsNoTracking()
            .OrderByDescending(i => i.OpprettetTid)
            .ToListAsync();

        return View(liste);
    }

    // Lager en ny lenke. Selve lenken vises bare én gang, rett etterpå,
    // fordi databasen bare har fingeravtrykket av koden (se Services/Invitasjonsregler.cs).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LagInvitasjon(string rolle, int dager)
    {
        // Godtar bare rollene og antall dager fra listene, i tilfelle noen har endret skjemaet.
        if (!Invitasjonsregler.Roller.Contains(rolle) || !Invitasjonsregler.GyldigDager.Contains(dager))
        {
            return BadRequest();
        }

        string kode = Invitasjonsregler.LagKode();

        _db.Invitasjoner.Add(new Invitasjon
        {
            KodeHash = Invitasjonsregler.LagHash(kode),
            Rolle = rolle,
            OpprettetAvBrukerId = _userManager.GetUserId(User),
            OpprettetTid = DateTime.UtcNow,
            UtloperTid = DateTime.UtcNow.AddDays(dager)
        });
        await _db.SaveChangesAsync();

        // Hele adressen, for eksempel https://.../Account/Invitasjon?kode=...
        TempData["NyLenke"] = Url.Action("Invitasjon", "Account", new { kode }, Request.Scheme);
        string varighet = T["{0} dager", dager].Value;
        if (dager == 1)
        {
            varighet = T["1 dag"].Value;
        }
        TempData["Melding"] = string.Format(T["Lenken gir rollen {0} og gjelder i {1}. Kopier den nå, den vises bare én gang."].Value, T[rolle].Value, varighet);
        return RedirectToAction(nameof(Invitasjoner));
    }

    // Trekker tilbake en lenke, så den ikke kan brukes lenger.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SlettInvitasjon(int id)
    {
        Invitasjon? invitasjon = await _db.Invitasjoner.FindAsync(id);
        if (invitasjon != null)
        {
            _db.Invitasjoner.Remove(invitasjon);
            await _db.SaveChangesAsync();
            TempData["Melding"] = T["Invitasjonen er slettet."].Value;
        }

        return RedirectToAction(nameof(Invitasjoner));
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

        IdentityUser? bruker = await _userManager.FindByIdAsync(id);
        if (bruker == null)
        {
            TempData["Feil"] = T["Fant ikke brukeren."].Value;
        }

        return bruker;
    }
}
