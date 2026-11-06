using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.Services;
using Nabohjelp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [Authorize(Roles = "Admin,Kommune,Frivillig")]
    public IActionResult Opprett()
    {
        var skjema = new BehovViewModel();

        // Brukernavnet er e-postadressen (se AccountController.Register).
        string? brukernavn = User.Identity?.Name;
        if (!string.IsNullOrEmpty(brukernavn) && new EmailAddressAttribute().IsValid(brukernavn))
        {
            skjema.KontaktEpost = brukernavn;
        }

        return View(skjema);
    }

    // POST: /Behov/Opprett
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Kommune,Frivillig")]
    public async Task<IActionResult> Opprett(BehovViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var behov = new Behov
        {
            Type = vm.Type!.Value,
            Beskrivelse = vm.Beskrivelse,
            AnnetType = vm.AnnetTypeForLagring(),
            Latitude = vm.Latitude!.Value,
            Longitude = vm.Longitude!.Value,
            Prioritet = vm.Prioritet!.Value,
            // Lagres med landskode, for eksempel «+47 12345678».
            KontaktTelefon = vm.TelefonForLagring(),
            KontaktEpost = vm.KontaktEpost,
            Status = BehovStatus.New,
            OpprettetTid = DateTime.UtcNow,
            OpprettetAvBrukerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        _db.Behovsliste.Add(behov);
        await _db.SaveChangesAsync();

        // Admin og Kommune sendes til behovskøen.
        if (Tilgang.ErAnsatt(User))
        {
            return RedirectToAction(nameof(Ko));
        }

        // Frivillige får bekreftelse og en knapp til Mine behov.
        TempData["Melding"] = "Ditt behov er registrert.";
        TempData["VisMineBehovKnapp"] = true;

        return RedirectToAction("Index", "Home");
    }

    // GET: /Behov/MineBehov
    // Frivillige ser bare egne behov.
    // Admin og Kommune ser alle behov.
    [HttpGet]
    [Authorize(Roles = "Admin,Kommune,Frivillig")]
    public async Task<IActionResult> MineBehov()
    {
        var brukerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(brukerId))
        {
            return Challenge();
        }

        var sporring = _db.Behovsliste.AsNoTracking();

        // Begrens til egne behov for Frivillig.
        if (!Tilgang.ErAnsatt(User))
        {
            sporring = sporring.Where(
                b => b.OpprettetAvBrukerId == brukerId);
        }

        var behov = await sporring
            .OrderByDescending(b => b.OpprettetTid)
            .ToListAsync();

        return View(behov);
    }

    // GET: /Behov/Rediger/5
    // Den som meldte behovet, Kommune og Admin kan endre det så lenge det ikke er tildelt.
    [HttpGet]
    [Authorize(Roles = "Admin,Kommune,Frivillig")]
    public async Task<IActionResult> Rediger(int id)
    {
        var behov = await _db.Behovsliste
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (behov is null)
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
    [Authorize(Roles = "Admin,Kommune,Frivillig")]
    public async Task<IActionResult> Rediger(int id, BehovViewModel vm)
    {
        var behov = await _db.Behovsliste.FindAsync(id);

        if (behov is null)
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

        behov.Type = vm.Type!.Value;
        behov.AnnetType = vm.AnnetTypeForLagring();
        behov.Beskrivelse = vm.Beskrivelse;
        behov.Latitude = vm.Latitude!.Value;
        behov.Longitude = vm.Longitude!.Value;
        behov.Prioritet = vm.Prioritet!.Value;
        behov.KontaktTelefon = vm.TelefonForLagring();
        behov.KontaktEpost = vm.KontaktEpost;
        behov.SistEndretTid = DateTime.UtcNow;
        behov.SistEndretAvBrukerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _db.SaveChangesAsync();

        TempData["Melding"] = "Endringene er lagret.";
        return TilbakeEtterRedigering();
    }

    // Den som meldte behovet, og kriseledelsen (Admin og Kommune).
    private bool KanEndre(Behov behov)
    {
        if (Tilgang.ErAnsatt(User))
        {
            return true;
        }

        var brukerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(brukerId) && behov.OpprettetAvBrukerId == brukerId;
    }

    private static string IkkeRedigerbarMelding(BehovStatus status)
    {
        return $"Behovet er «{Visningstekst.Status(status)}» og kan ikke endres lenger. " +
               "Kontakt kommunen hvis noe har endret seg.";
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
    [Authorize(Roles = "Admin,Kommune,Frivillig")]
    public async Task<IActionResult> TrekkTilbake(int id)
    {
        var behov = await _db.Behovsliste.FindAsync(id);

        if (behov is null)
        {
            return NotFound();
        }

        // Bare den som meldte behovet, kan trekke det tilbake.
        var brukerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(brukerId) || behov.OpprettetAvBrukerId != brukerId)
        {
            return Forbid();
        }

        if (!BehovStatusflyt.KanTrekkesTilbake(behov.Status))
        {
            TempData["Feil"] =
                $"Behovet er «{Visningstekst.Status(behov.Status)}» og kan ikke trekkes tilbake. " +
                "Kontakt kommunen hvis du ikke trenger hjelpen lenger.";

            return RedirectToAction(nameof(MineBehov));
        }

        behov.Status = BehovStatus.Withdrawn;
        behov.SistEndretTid = DateTime.UtcNow;
        behov.SistEndretAvBrukerId = brukerId;

        await _db.SaveChangesAsync();

        TempData["Melding"] = "Behovet er trukket tilbake.";
        return RedirectToAction(nameof(MineBehov));
    }

    // GET: /Behov/Ko
    // Kun Admin og Kommune har tilgang.
    // Avsluttede behov (løst, avvist, trukket tilbake) vises ikke.
    [HttpGet]
    [Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
    public async Task<IActionResult> Ko()
    {
        var liste = await _db.Behovsliste
            .Where(b => b.Status != BehovStatus.Resolved &&
                        b.Status != BehovStatus.Rejected &&
                        b.Status != BehovStatus.Withdrawn)
            .OrderBy(b => b.Prioritet)
            .ThenBy(b => b.OpprettetTid)
            .ToListAsync();

        var visning = liste.Select(b => new BehovKoItem
        {
            Id = b.Id,
            Type = b.Type,
            AnnetType = b.AnnetType,
            Prioritet = b.Prioritet,
            Status = b.Status,
            Beskrivelse = b.Beskrivelse,
            OpprettetTid = b.OpprettetTid,
            KontaktTelefon = b.KontaktTelefon,
            KontaktEpost = b.KontaktEpost,
            MuligeNesteStatuser =
                BehovStatusflyt.NesteStatuser(b.Status)
        }).ToList();

        return View(visning);
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
        var behov = await _db.Behovsliste.FindAsync(id);

        if (behov is null)
        {
            return NotFound();
        }

        if (!BehovStatusflyt.KanEndres(behov.Status, nyStatus))
        {
            TempData["Feil"] =
                $"Kan ikke endre status fra «{Visningstekst.Status(behov.Status)}» " +
                $"til «{Visningstekst.Status(nyStatus)}».";

            return RedirectToAction(nameof(Ko));
        }

        // Endringene lagres samlet, så ressursene ikke blir frigjort
        // hvis lagringen av behovet feiler.
        await using var transaksjon =
            await _db.Database.BeginTransactionAsync();

        int antallFrigjort = 0;

        // Når behovet er løst, trengs ikke ressursene lenger.
        // De blir «Tilgjengelig» igjen, så de kan tildeles nye behov.
        if (nyStatus == BehovStatus.Resolved)
        {
            antallFrigjort = await _db.Ressurser
                .Where(r => r.Status == "Tildelt" &&
                            _db.Tildelinger.Any(t => t.BehovId == id && t.RessursId == r.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Status, "Tilgjengelig"));
        }

        behov.Status = nyStatus;
        behov.SistEndretTid = DateTime.UtcNow;
        behov.SistEndretAvBrukerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _db.SaveChangesAsync();
        await transaksjon.CommitAsync();

        TempData["Melding"] = LagStatusMelding(nyStatus, antallFrigjort);
        return RedirectToAction(nameof(Ko));
    }

    // Bekreftelsen som vises øverst i køen etter en statusendring.
    private static string LagStatusMelding(BehovStatus nyStatus, int antallFrigjort)
    {
        string melding = $"Behovet har nå status «{Visningstekst.Status(nyStatus)}».";

        if (antallFrigjort == 1)
        {
            melding += " Ressursen som var tildelt, er tilgjengelig igjen.";
        }
        else if (antallFrigjort > 1)
        {
            melding += $" {antallFrigjort} ressurser som var tildelt, er tilgjengelige igjen.";
        }

        return melding;
    }
}
