using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.Services;
using Nabohjelp.ViewModels;
using Microsoft.AspNetCore.Authorization;
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

    public BehovController(ApplicationDbContext db)
    {
        _db = db;
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
    [HttpGet]
    public async Task<IActionResult> MineBehov()
    {
        // Spørringen mot databasen bygges opp i flere steg og kjøres først ved ToListAsync.
        // b => ... betyr «for hvert behov b»: Where velger behov, OrderByDescending sorterer.
        IQueryable<Behov> sporring = _db.Behovsliste.AsNoTracking();

        // Frivillige ser bare sine egne behov.
        if (!Tilgang.ErAnsatt(User))
        {
            string brukerId = Tilgang.BrukerId(User);
            sporring = sporring.Where(b => b.OpprettetAvBrukerId == brukerId);
        }

        // Nyeste først.
        List<Behov> behovsliste = await sporring
            .OrderByDescending(b => b.OpprettetTid)
            .ToListAsync();

        return View(behovsliste);
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

        behov.Status = BehovStatus.Withdrawn;
        behov.SistEndretTid = DateTime.UtcNow;
        behov.SistEndretAvBrukerId = Tilgang.BrukerId(User);

        await _db.SaveChangesAsync();

        TempData["Melding"] = Oversetter.Tekst("Behovet er trukket tilbake.");
        return RedirectToAction(nameof(MineBehov));
    }

    // GET: /Behov/Ko           (aktive behov)
    // GET: /Behov/Ko?visning=alle (alle behov, også løste, avviste og trukket tilbake)
    // Kun Admin og Kommune har tilgang.
    [HttpGet]
    [Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
    public async Task<IActionResult> Ko(string? visning)
    {
        bool visAlle = visning == "alle";
        IQueryable<Behov> sporring = _db.Behovsliste.AsNoTracking();

        // Vanligvis vises bare behov som ikke er avsluttet.
        if (!visAlle)
        {
            sporring = sporring.Where(b => b.Status != BehovStatus.Resolved &&
                                           b.Status != BehovStatus.Rejected &&
                                           b.Status != BehovStatus.Withdrawn);
        }

        // Akutte først (Urgent = 0), deretter de eldste først.
        List<Behov> behovsliste = await sporring
            .OrderBy(b => b.Prioritet)
            .ThenBy(b => b.OpprettetTid)
            .ToListAsync();

        ViewBag.VisAlle = visAlle;

        // Lager én rad i køen for hvert behov.
        var rader = new List<BehovKoItem>();
        foreach (Behov behov in behovsliste)
        {
            rader.Add(new BehovKoItem
            {
                Id = behov.Id,
                Type = behov.Type,
                AnnetType = behov.AnnetType,
                Prioritet = behov.Prioritet,
                Status = behov.Status,
                Beskrivelse = behov.Beskrivelse,
                OpprettetTid = behov.OpprettetTid,
                KontaktTelefon = behov.KontaktTelefon,
                KontaktEpost = behov.KontaktEpost,
                MuligeNesteStatuser = BehovStatusflyt.NesteStatuser(behov.Status)
            });
        }

        return View(rader);
    }

    // POST: /Behov/EndreStatus
    // Kun Admin og Kommune kan endre status.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
    public async Task<IActionResult> EndreStatus(
        int id,
        BehovStatus nyStatus)
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

            return RedirectToAction(nameof(Ko));
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

        behov.Status = nyStatus;
        behov.SistEndretTid = DateTime.UtcNow;
        behov.SistEndretAvBrukerId = Tilgang.BrukerId(User);

        // SaveChangesAsync lagrer behovet og ressursene samtidig. Feiler noe,
        // lagres ingenting, så ressursene blir aldri frigjort uten at behovet er løst.
        await _db.SaveChangesAsync();

        TempData["Melding"] = LagStatusMelding(nyStatus, antallFrigjort);
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
}
