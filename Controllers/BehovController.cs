using Microsoft.AspNetCore.SignalR;
using Nabohjelp.Hubs;
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

    // Brukes til å varsle nettleserne når behov endres.
    private readonly IHubContext<OppdateringsHub> _hub;

    public BehovController(
        ApplicationDbContext db,
        UserManager<IdentityUser> userManager,
        IHubContext<OppdateringsHub> hub)
    {
        _db = db;
        _userManager = userManager;
        _hub = hub;
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

        if (!string.IsNullOrEmpty(brukernavn) &&
            new EmailAddressAttribute().IsValid(brukernavn))
        {
            skjema.KontaktEpost = brukernavn;
        }

        return View(skjema);
    }

    // POST: /Behov/Opprett
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(Sikkerhet.NyRegistrering)]
    public async Task<IActionResult> Opprett(BehovViewModel vm)
    {
        // Skjult felt fylt ut = robot.
        if (Sikkerhet.ErRobot(Request))
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // Status, tidspunkt og eier settes her.
        var behov = new Behov
        {
            Status = BehovStatus.New,
            OpprettetTid = DateTime.UtcNow,
            OpprettetAvBrukerId = Tilgang.BrukerId(User)
        };

        vm.KopierTil(behov);

        // Lagrer behovet én gang.
        _db.Behovsliste.Add(behov);
        await _db.SaveChangesAsync();

        // Varsler alle tilkoblede nettlesere om at et nytt behov er registrert.
        await _hub.Clients.All.SendAsync("BehovOppdatert");

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
    [HttpGet]
    public async Task<IActionResult> MineBehov(BehovListeViewModel filter)
    {
        IQueryable<Behov> sporring = _db.Behovsliste.AsNoTracking();

        // Frivillige ser bare sine egne behov.
        if (!Tilgang.ErAnsatt(User))
        {
            string brukerId = Tilgang.BrukerId(User);

            sporring = sporring.Where(
                b => b.OpprettetAvBrukerId == brukerId);
        }

        // Viser alle kategorier hvis ingen er valgt.
        filter.Fyll(
            await sporring.ToListAsync(),
            BehovStatusflyt.KategoriAlle);

        await LeggTilHistorikk(filter);

        return View(filter);
    }

    // GET: /Behov/Rediger/5
    // Eier, Kommune og Admin kan redigere behovet så lenge det ikke er tildelt.
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

        // Sjekker at behovet fortsatt kan redigeres.
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

        // Varsler alle tilkoblede nettlesere om at behovet er endret.
        await _hub.Clients.All.SendAsync("BehovOppdatert");

        TempData["Melding"] = Oversetter.Tekst("Endringene er lagret.");

        return TilbakeEtterRedigering();
    }

    // Kontrollerer om brukeren har lov til å endre behovet.
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

    // Kriseledelsen går tilbake til køen.
    // Andre brukere går tilbake til Mine behov.
    private IActionResult TilbakeEtterRedigering()
    {
        if (Tilgang.ErAnsatt(User))
        {
            return RedirectToAction(nameof(Ko));
        }

        return RedirectToAction(nameof(MineBehov));
    }

    // POST: /Behov/TrekkTilbake/5
    // Eier kan trekke tilbake behovet så lenge det ikke er tildelt.
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

        _db.StatusEndringer.Add(
            StatusEndring.Ny(
                behov.Id,
                behov.Status,
                BehovStatus.Withdrawn,
                brukerId));

        behov.Status = BehovStatus.Withdrawn;
        behov.SistEndretTid = DateTime.UtcNow;
        behov.SistEndretAvBrukerId = brukerId;

        await _db.SaveChangesAsync();

        // Varsler alle tilkoblede nettlesere om at behovet er trukket tilbake.
        await _hub.Clients.All.SendAsync("BehovOppdatert");

        TempData["Melding"] = Oversetter.Tekst("Behovet er trukket tilbake.");

        return RedirectToAction(nameof(MineBehov));
    }

    // GET: /Behov/Ko
    // Kun Admin og Kommune har tilgang.
    [HttpGet]
    [Authorize(Policy = "Ansatt")]
    public async Task<IActionResult> Ko(BehovListeViewModel filter)
    {
        List<Behov> alle = await _db.Behovsliste
            .AsNoTracking()
            .ToListAsync();

        filter.Fyll(alle, BehovStatusflyt.KategoriApne);

        await LeggTilHistorikk(filter);

        return View(filter);
    }

    // POST: /Behov/EndreStatus
    // Kun Admin og Kommune kan endre status.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Ansatt")]
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

        // Når behovet er løst, blir tildelte ressurser tilgjengelige igjen.
        if (nyStatus == BehovStatus.Resolved)
        {
            List<Tildeling> tildelinger = await _db.Tildelinger
                .Where(t => t.BehovId == id)
                .ToListAsync();

            foreach (Tildeling tildeling in tildelinger)
            {
                Ressurs? ressurs = await _db.Ressurser.FindAsync(
                    tildeling.RessursId);

                if (ressurs != null && ressurs.Status == "Tildelt")
                {
                    ressurs.Status = "Tilgjengelig";
                    antallFrigjort++;
                }
            }
        }

        string brukerId = Tilgang.BrukerId(User);

        _db.StatusEndringer.Add(
            StatusEndring.Ny(
                behov.Id,
                behov.Status,
                nyStatus,
                brukerId));

        behov.Status = nyStatus;
        behov.SistEndretTid = DateTime.UtcNow;
        behov.SistEndretAvBrukerId = brukerId;

        // Lagrer behovet, historikken og eventuelle ressursendringer samlet.
        await _db.SaveChangesAsync();

        // Varsler alle nettlesere om at behovet har fått ny status.
        await _hub.Clients.All.SendAsync("BehovOppdatert");

        // Varsler også om ressursendringer.
        // Dette er viktig når ressurser frigjøres.
        await _hub.Clients.All.SendAsync("RessurserOppdatert");

        // Viser bekreftelse og sender brukeren tilbake til behovskøen.
        TempData["Melding"] = LagStatusMelding(
            nyStatus,
            antallFrigjort);

        return TilbakeTilKoen(tilbake);
    }

    // Sender brukeren tilbake til behovskøen med samme filter.
    // Godtar bare lokale adresser.
    private IActionResult TilbakeTilKoen(string? tilbake)
    {
        if (!string.IsNullOrEmpty(tilbake) && Url.IsLocalUrl(tilbake))
        {
            return LocalRedirect(tilbake);
        }

        return RedirectToAction(nameof(Ko));
    }

    // Lager bekreftelsen som vises etter en statusendring.
    private static string LagStatusMelding(
        BehovStatus nyStatus,
        int antallFrigjort)
    {
        string melding = Oversetter.Tekst(
            "Behovet har nå status «{0}».",
            Visningstekst.Status(nyStatus));

        if (antallFrigjort == 1)
        {
            melding += " " + Oversetter.Tekst(
                "Ressursen som var tildelt, er tilgjengelig igjen.");
        }
        else if (antallFrigjort > 1)
        {
            melding += " " + Oversetter.Tekst(
                "{0} ressurser som var tildelt, er tilgjengelige igjen.",
                antallFrigjort);
        }

        return melding;
    }

    // Henter historikken til behovene som vises.
    private async Task LeggTilHistorikk(BehovListeViewModel modell)
    {
        var behovIder = new List<int>();

        foreach (Behov behov in modell.Liste)
        {
            behovIder.Add(behov.Id);
        }

        // Henter statusendringer, eldste først.
        modell.Historikk = await _db.StatusEndringer
            .AsNoTracking()
            .Where(s => behovIder.Contains(s.BehovId))
            .OrderBy(s => s.Tid)
            .ToListAsync();

        // Frivillige skal ikke se hvem i kommunen som gjorde endringene.
        if (!Tilgang.ErAnsatt(User))
        {
            return;
        }

        // Henter brukernavnene til dem som har endret status.
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

        // Henter ressurser som er tildelt behovene.
        List<Tildeling> tildelinger = await _db.Tildelinger
            .AsNoTracking()
            .Where(t => behovIder.Contains(t.BehovId))
            .ToListAsync();

        var ressursIder = new List<int>();

        foreach (Tildeling tildeling in tildelinger)
        {
            ressursIder.Add(tildeling.RessursId);
        }

        List<Ressurs> ressurser = await _db.Ressurser
            .AsNoTracking()
            .Where(r => ressursIder.Contains(r.Id))
            .ToListAsync();

        foreach (Tildeling tildeling in tildelinger)
        {
            Behov? behov = FinnBehov(
                modell.Liste,
                tildeling.BehovId);

            Ressurs? ressurs = FinnRessurs(
                ressurser,
                tildeling.RessursId);

            if (behov != null &&
                ressurs != null &&
                BehovStatusflyt.KanAngreTildeling(behov.Status))
            {
                modell.Tildelte.Add(
                    new TildeltRessursViewModel
                    {
                        TildelingId = tildeling.Id,
                        BehovId = tildeling.BehovId,
                        Ressurs = ressurs
                    });
            }
        }
    }

    // Finner et behov i listen basert på ID.
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

    // Finner en ressurs i listen basert på ID.
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