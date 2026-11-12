using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.Services;
using Nabohjelp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Nabohjelp.Controllers;

// Behov: registrering, køen for kriseledelsen og statusflyten.
// Reglene for hvilke statuser et behov kan gå til, ligger i Models/BehovStatusflyt.cs.
[Authorize]
public class BehovController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public BehovController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // GET: /Behov/Opprett
    // Alle innloggede roller kan melde behov (Admin, Kommune og Frivillig).
    // E-posten fra kontoen fylles inn som kontaktinfo, men kan endres.
    [HttpGet]
    public IActionResult Opprett()
    {
        var skjema = new BehovViewModel();

        // Brukernavnet er e-postadressen (se AccountController.Register).
        string? brukernavn = User.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrEmpty(brukernavn) && new EmailAddressAttribute().IsValid(brukernavn))
        {
            skjema.KontaktEpost = brukernavn;
        }

        return View(skjema);
    }

    // POST: /Behov/Opprett
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(Sikkerhet.NyRegistrering)] // maks 20 per minutt per bruker (se Services/Sikkerhet.cs)
    public async Task<IActionResult> Opprett(BehovViewModel vm)
    {
        // Skjult felt fylt ut = robot (se Services/Sikkerhet.cs).
        if (Sikkerhet.ErRobot(Request))
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // Status, tidspunkt og eier settes her. Resten kommer fra skjemaet.
        var behov = new Behov
        {
            Status = BehovStatus.New,
            OpprettetTid = DateTime.UtcNow,
            OpprettetAvBrukerId = Tilgang.BrukerId(User)
        };
        vm.KopierTil(behov);

        _db.Behovsliste.Add(behov);
        await _db.SaveChangesAsync();

        // Admin og Kommune sendes til behovskøen.
        if (Tilgang.ErAnsatt(User))
        {
            return RedirectToAction(nameof(Ko));
        }

        // Frivillige får bekreftelse og en knapp til Mine behov.
        TempData["Melding"] = Oversetter.Tekst("Ditt behov er registrert.");
        TempData["VisMineBehovKnapp"] = true;

        return RedirectToAction("Index", "Home");
    }

    // GET: /Behov/MineBehov
    // Frivillige ser bare egne behov.
    // Admin og Kommune ser alle behov.
    // Kategori og filter kommer fra adressen (se ViewModels/BehovListeViewModel.cs).
    [HttpGet]
    public async Task<IActionResult> MineBehov(BehovListeViewModel filter)
    {
        // Spørringen mot databasen bygges opp i flere steg og kjøres først ved ToListAsync.
        // b => ... betyr «for hvert behov b»: Where velger behov.
        IQueryable<Behov> sporring = _db.Behovsliste.AsNoTracking();

        // Frivillige ser bare sine egne behov.
        if (!Tilgang.ErAnsatt(User))
        {
            string brukerId = Tilgang.BrukerId(User);
            sporring = sporring.Where(b => b.OpprettetAvBrukerId == brukerId);
        }

        // Her vises alle kategorier hvis ingen er valgt.
        filter.Fyll(await sporring.ToListAsync(), BehovStatusflyt.KategoriAlle);
        await LeggTilHistorikk(filter);
        return View(filter);
    }

    // GET: /Behov/Rediger/5
    // Den som meldte behovet, Kommune og Admin kan endre det så lenge det ikke er tildelt.
    [HttpGet]
    public async Task<IActionResult> Rediger(int id)
    {
        Behov? behov = await _db.Behovsliste.FindAsync(id);

        if (behov == null)
        {
            return NotFound();
        }

        if (!KanEndre(behov))
        {
            return Forbid();
        }

        if (!BehovStatusflyt.KanRedigeres(behov.Status))
        {
            TempData["Feil"] = IkkeRedigerbarMelding(behov.Status);
            return TilbakeEtterRedigering();
        }

        return View("Opprett", BehovViewModel.FraBehov(behov));
    }

    // POST: /Behov/Rediger/5
    // Status, tidspunkt og eier endres aldri fra skjemaet.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rediger(int id, BehovViewModel vm)
    {
        Behov? behov = await _db.Behovsliste.FindAsync(id);

        if (behov == null)
        {
            return NotFound();
        }

        if (!KanEndre(behov))
        {
            return Forbid();
        }

        // Sjekkes på nytt: behovet kan ha blitt tildelt mens skjemaet var åpent.
        if (!BehovStatusflyt.KanRedigeres(behov.Status))
        {
            TempData["Feil"] = IkkeRedigerbarMelding(behov.Status);
            return TilbakeEtterRedigering();
        }

        if (!ModelState.IsValid)
        {
            vm.Id = id;
            return View("Opprett", vm);
        }

        vm.KopierTil(behov);
        behov.SistEndretTid = DateTime.UtcNow;
        behov.SistEndretAvBrukerId = Tilgang.BrukerId(User);

        await _db.SaveChangesAsync();

        TempData["Melding"] = Oversetter.Tekst("Endringene er lagret.");
        return TilbakeEtterRedigering();
    }

    // Den som meldte behovet, og kriseledelsen (Admin og Kommune).
    private bool KanEndre(Behov behov)
    {
        return Tilgang.KanEndre(User, behov.OpprettetAvBrukerId);
    }

    private static string IkkeRedigerbarMelding(BehovStatus status)
    {
        return Oversetter.Tekst(
            "Behovet er «{0}» og kan ikke endres lenger. Kontakt kommunen hvis noe har endret seg.",
            Visningstekst.Status(status));
    }

    // Kriseledelsen går tilbake til køen, den som meldte behovet til «Mine behov».
    private IActionResult TilbakeEtterRedigering()
    {
        if (Tilgang.ErAnsatt(User))
        {
            return RedirectToAction(nameof(Ko));
        }

        return RedirectToAction(nameof(MineBehov));
    }

    // POST: /Behov/TrekkTilbake/5
    // Den som meldte behovet kan trekke det tilbake, så lenge det ikke er tildelt.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrekkTilbake(int id)
    {
        Behov? behov = await _db.Behovsliste.FindAsync(id);

        if (behov == null)
        {
            return NotFound();
        }

        // Bare den som meldte behovet, kan trekke det tilbake.
        if (!Tilgang.ErEier(User, behov.OpprettetAvBrukerId))
        {
            return Forbid();
        }

        if (!BehovStatusflyt.KanRedigeres(behov.Status))
        {
            TempData["Feil"] = Oversetter.Tekst(
                "Behovet er «{0}» og kan ikke trekkes tilbake. Kontakt kommunen hvis du ikke trenger hjelpen lenger.",
                Visningstekst.Status(behov.Status));

            return RedirectToAction(nameof(MineBehov));
        }

        string brukerId = Tilgang.BrukerId(User);
        _db.StatusEndringer.Add(StatusEndring.Ny(behov.Id, behov.Status, BehovStatus.Withdrawn, brukerId));

        behov.Status = BehovStatus.Withdrawn;
        behov.SistEndretTid = DateTime.UtcNow;
        behov.SistEndretAvBrukerId = brukerId;

        await _db.SaveChangesAsync();

        TempData["Melding"] = Oversetter.Tekst("Behovet er trukket tilbake.");
        return RedirectToAction(nameof(MineBehov));
    }

    // GET: /Behov/Ko
    // Kun Admin og Kommune har tilgang. Viser åpne behov hvis ingen kategori er valgt.
    // Kategori og filter kommer fra adressen (se ViewModels/BehovListeViewModel.cs).
    [HttpGet]
    [Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
    public async Task<IActionResult> Ko(BehovListeViewModel filter)
    {
        List<Behov> alle = await _db.Behovsliste.AsNoTracking().ToListAsync();
        filter.Fyll(alle, BehovStatusflyt.KategoriApne);
        await LeggTilHistorikk(filter);
        return View(filter);
    }

    // POST: /Behov/EndreStatus
    // Kun Admin og Kommune kan endre status.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
    public async Task<IActionResult> EndreStatus(
        int id,
        BehovStatus nyStatus,
        string? tilbake)
    {
        Behov? behov = await _db.Behovsliste.FindAsync(id);

        if (behov == null)
        {
            return NotFound();
        }

        if (!BehovStatusflyt.KanEndres(behov.Status, nyStatus))
        {
            TempData["Feil"] = Oversetter.Tekst(
                "Kan ikke endre status fra «{0}» til «{1}».",
                Visningstekst.Status(behov.Status),
                Visningstekst.Status(nyStatus));

            return TilbakeTilKoen(tilbake);
        }

        int antallFrigjort = 0;

        // Når behovet er løst, trengs ikke ressursene lenger.
        // De blir «Tilgjengelig» igjen, så de kan tildeles nye behov.
        if (nyStatus == BehovStatus.Resolved)
        {
            // Alle tildelingene til dette behovet (t => ... betyr «for hver tildeling t»).
            List<Tildeling> tildelinger = await _db.Tildelinger
                .Where(t => t.BehovId == id)
                .ToListAsync();

            foreach (Tildeling tildeling in tildelinger)
            {
                Ressurs? ressurs = await _db.Ressurser.FindAsync(tildeling.RessursId);

                if (ressurs != null && ressurs.Status == "Tildelt")
                {
                    ressurs.Status = "Tilgjengelig";
                    antallFrigjort++;
                }
            }
        }

        string brukerId = Tilgang.BrukerId(User);
        _db.StatusEndringer.Add(StatusEndring.Ny(behov.Id, behov.Status, nyStatus, brukerId));

        behov.Status = nyStatus;
        behov.SistEndretTid = DateTime.UtcNow;
        behov.SistEndretAvBrukerId = brukerId;

        // SaveChangesAsync lagrer behovet, historikken og ressursene samtidig. Feiler noe,
        // lagres ingenting, så ressursene blir aldri frigjort uten at behovet er løst.
        await _db.SaveChangesAsync();

        TempData["Melding"] = LagStatusMelding(nyStatus, antallFrigjort);
        return TilbakeTilKoen(tilbake);
    }

    // Tilbake til køen med samme kategori og filter som før (adressen kommer fra skjemaet).
    // Bare adresser på vår egen side godtas, så lenken ikke kan sende brukeren til en annen nettside.
    private IActionResult TilbakeTilKoen(string? tilbake)
    {
        if (!string.IsNullOrEmpty(tilbake) && Url.IsLocalUrl(tilbake))
        {
            return LocalRedirect(tilbake);
        }
        return RedirectToAction(nameof(Ko));
    }

    // Bekreftelsen som vises øverst i køen etter en statusendring.
    private static string LagStatusMelding(BehovStatus nyStatus, int antallFrigjort)
    {
        string melding = Oversetter.Tekst("Behovet har nå status «{0}».", Visningstekst.Status(nyStatus));

        if (antallFrigjort == 1)
        {
            melding += " " + Oversetter.Tekst("Ressursen som var tildelt, er tilgjengelig igjen.");
        }
        else if (antallFrigjort > 1)
        {
            melding += " " + Oversetter.Tekst("{0} ressurser som var tildelt, er tilgjengelige igjen.", antallFrigjort);
        }

        return melding;
    }

    // Henter historikken til behovene som vises (se Models/StatusEndring.cs).
    // Kriseledelsen får i tillegg se hvem som gjorde endringene, og hvilke ressurser
    // som er tildelt nå (de vises i behovskøen med knappen «Angre tildeling»).
    private async Task LeggTilHistorikk(BehovListeViewModel modell)
    {
        var behovIder = new List<int>();
        foreach (Behov behov in modell.Liste)
        {
            behovIder.Add(behov.Id);
        }

        // Alle endringene for disse behovene, eldste først (s => ... betyr «for hver endring s»).
        modell.Historikk = await _db.StatusEndringer.AsNoTracking()
            .Where(s => behovIder.Contains(s.BehovId))
            .OrderBy(s => s.Tid)
            .ToListAsync();

        // Frivillige ser historikken til sine egne behov, men ikke hvem i kommunen som gjorde hva.
        if (!Tilgang.ErAnsatt(User))
        {
            return;
        }

        // Slår opp e-postadressen én gang for hver bruker som har endret noe.
        foreach (StatusEndring endring in modell.Historikk)
        {
            string? id = endring.EndretAvBrukerId;
            if (id != null && !modell.Brukernavn.ContainsKey(id))
            {
                IdentityUser? bruker = await _userManager.FindByIdAsync(id);
                if (bruker != null && bruker.Email != null)
                {
                    modell.Brukernavn[id] = bruker.Email;
                }
            }
        }

        // Ressursene som er tildelt behovene nå. Etter at et behov er løst, er tildelingen
        // historikk og kan ikke angres, så vi tar bare med behov som er «Tildelt».
        List<Tildeling> tildelinger = await _db.Tildelinger.AsNoTracking()
            .Where(t => behovIder.Contains(t.BehovId))
            .ToListAsync();

        var ressursIder = new List<int>();
        foreach (Tildeling tildeling in tildelinger)
        {
            ressursIder.Add(tildeling.RessursId);
        }

        List<Ressurs> ressurser = await _db.Ressurser.AsNoTracking()
            .Where(r => ressursIder.Contains(r.Id))
            .ToListAsync();

        foreach (Tildeling tildeling in tildelinger)
        {
            Behov? behov = FinnBehov(modell.Liste, tildeling.BehovId);
            Ressurs? ressurs = FinnRessurs(ressurser, tildeling.RessursId);

            if (behov != null && ressurs != null && BehovStatusflyt.KanAngreTildeling(behov.Status))
            {
                modell.Tildelte.Add(new TildeltRessursViewModel
                {
                    TildelingId = tildeling.Id,
                    BehovId = tildeling.BehovId,
                    Ressurs = ressurs
                });
            }
        }
    }

    private static Behov? FinnBehov(List<Behov> liste, int id)
    {
        foreach (Behov behov in liste)
        {
            if (behov.Id == id)
            {
                return behov;
            }
        }
        return null;
    }

    private static Ressurs? FinnRessurs(List<Ressurs> liste, int id)
    {
        foreach (Ressurs ressurs in liste)
        {
            if (ressurs.Id == id)
            {
                return ressurs;
            }
        }
        return null;
    }
}
