using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.Services;
using Nabohjelp.ViewModels;
using System.Security.Claims;

namespace Nabohjelp.Controllers;

// Bare Admin og Kommune kan bruke matching og tildeling.
[Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
public class TildelingController : Controller
{
    private readonly ApplicationDbContext _db;

    public TildelingController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Tildeling
    // Viser behov som kan matches og tildeles.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Avsluttede behov kan ikke få flere ressurser.
        var behov = await _db.Behovsliste
            .Where(b => b.Status != BehovStatus.Resolved &&
                        b.Status != BehovStatus.Rejected &&
                        b.Status != BehovStatus.Withdrawn)
            .OrderBy(b => b.Prioritet)
            .ThenBy(b => b.OpprettetTid)
            .ToListAsync();

        return View(behov);
    }

    // GET: /Tildeling/Match/5
    // Finner tilgjengelige ressurser som passer til behovet.
    [HttpGet]
    public async Task<IActionResult> Match(int id)
    {
        var behov = await _db.Behovsliste.FindAsync(id);

        if (behov == null)
        {
            return NotFound();
        }

        // Et avsluttet behov (for eksempel via en gammel lenke) skal ikke vise tildel-knapper.
        if (BehovStatusflyt.ErAvsluttet(behov.Status))
        {
            TempData["Feil"] = $"Behovet er «{Visningstekst.Status(behov.Status)}» og kan ikke få flere ressurser.";
            return RedirectToAction(nameof(Index));
        }

        // Henter bare tilgjengelige ressurser fra databasen.
        var ressurser = await _db.Ressurser
            .AsNoTracking()
            .Where(r => r.Status == "Tilgjengelig")
            .ToListAsync();

        // Matcher etter ressurstype, beregner avstand og sorterer.
        var matchendeRessurser = ressurser
            .Where(r => PasserTilBehov(behov.Type, r.Type))
            .Select(r => new MatchendeRessursViewModel
            {
                RessursId = r.Id,
                Type = r.Type,
                Posisjon = r.Posisjon,
                Status = r.Status,
                Navn = r.Navn,
                Beskrivelse = r.Beskrivelse,

                AvstandKm = Avstandsberegning.BeregnAvstandKm(
                    behov.Latitude,
                    behov.Longitude,
                    r.Breddegrad,
                    r.Lengdegrad),

                MatchBegrunnelse = LagMatchBegrunnelse(
                    behov.Type,
                    r.Type)
            })
            .OrderBy(r => r.AvstandKm ?? double.MaxValue)
            .ThenBy(r => r.Type)
            .ToList();

        var model = new TildelingViewModel
        {
            BehovId = behov.Id,
            BehovType = behov.Type,
            Prioritet = behov.Prioritet,
            Status = behov.Status,
            Beskrivelse = behov.Beskrivelse,
            Latitude = behov.Latitude,
            Longitude = behov.Longitude,
            MatchendeRessurser = matchendeRessurser
        };

        return View(model);
    }

    // POST: /Tildeling/Tildel
    // Tildeler en ressurs og lagrer koblingen til behovet.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tildel(int behovId, int ressursId)
    {
        var brukerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(brukerId))
        {
            return Challenge();
        }

        var behov = await _db.Behovsliste.FindAsync(behovId);
        var ressurs = await _db.Ressurser.FindAsync(ressursId);

        if (behov == null || ressurs == null)
        {
            return NotFound();
        }

        // Avsluttede behov kan ikke få nye tildelinger.
        if (BehovStatusflyt.ErAvsluttet(behov.Status))
        {
            TempData["Feil"] =
                $"Behovet er «{Visningstekst.Status(behov.Status)}» og kan ikke få flere ressurser.";

            return RedirectToAction(nameof(Index));
        }

        // Kontrollerer at ressurstypen passer til behovet.
        if (!PasserTilBehov(behov.Type, ressurs.Type))
        {
            TempData["Feil"] = "Ressursen passer ikke til dette behovet.";

            return RedirectToAction(
                nameof(Match),
                new { id = behovId });
        }

        // Transaksjon sørger for at endringene lagres samlet.
        await using var transaksjon =
            await _db.Database.BeginTransactionAsync();

        // Oppdaterer ressursen bare dersom den fortsatt er tilgjengelig.
        // Dette hindrer at to forespørsler tildeler samme ressurs samtidig.
        var antallEndret = await _db.Ressurser
            .Where(r => r.Id == ressursId &&
                        r.Status == "Tilgjengelig")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, "Tildelt"));

        if (antallEndret == 0)
        {
            await transaksjon.RollbackAsync();

            TempData["Feil"] =
                "Ressursen er ikke lenger tilgjengelig.";

            return RedirectToAction(
                nameof(Match),
                new { id = behovId });
        }

        var tidspunkt = DateTime.UtcNow;

        // Oppdaterer behovets status.
        behov.Status = BehovStatus.Assigned;
        behov.SistEndretTid = tidspunkt;
        behov.SistEndretAvBrukerId = brukerId;

        // Lagrer koblingen mellom behov, ressurs og brukeren.
        var tildeling = new Tildeling
        {
            BehovId = behovId,
            RessursId = ressursId,
            TildeltAvBrukerId = brukerId,
            TildeltTid = tidspunkt
        };

        _db.Tildelinger.Add(tildeling);

        await _db.SaveChangesAsync();

        // Bekrefter endringene i databasen.
        await transaksjon.CommitAsync();

        return RedirectToAction(
            nameof(Resultat),
            new { behovId, ressursId });
    }

    // GET: /Tildeling/Resultat
    // Viser resultatet etter en tildeling.
    [HttpGet]
    public async Task<IActionResult> Resultat(int behovId, int ressursId)
    {
        var behov = await _db.Behovsliste.FindAsync(behovId);
        var ressurs = await _db.Ressurser.FindAsync(ressursId);

        if (behov == null || ressurs == null)
        {
            return NotFound();
        }

        var model = new TildelingViewModel
        {
            BehovId = behov.Id,
            BehovType = behov.Type,
            Prioritet = behov.Prioritet,
            Status = behov.Status,
            Beskrivelse = behov.Beskrivelse,
            Latitude = behov.Latitude,
            Longitude = behov.Longitude,

            MatchendeRessurser = new List<MatchendeRessursViewModel>
            {
                new()
                {
                    RessursId = ressurs.Id,
                    Type = ressurs.Type,
                    Posisjon = ressurs.Posisjon,
                    Status = ressurs.Status,
                    Navn = ressurs.Navn,
                    Beskrivelse = ressurs.Beskrivelse,

                    AvstandKm = Avstandsberegning.BeregnAvstandKm(
                        behov.Latitude,
                        behov.Longitude,
                        ressurs.Breddegrad,
                        ressurs.Lengdegrad),

                    MatchBegrunnelse = LagMatchBegrunnelse(
                        behov.Type,
                        ressurs.Type)
                }
            }
        };

        return View(model);
    }

    // Bestemmer hvilke ressurstyper som passer til behovstypen.
    private static bool PasserTilBehov(
        BehovType behovType,
        string ressursType)
    {
        return behovType switch
        {
            BehovType.Drone =>
                ressursType == "Drone og droneoperatør",

            BehovType.Generator =>
                ressursType == "Aggregat",

            BehovType.Transport =>
                ressursType is
                    "ATV" or
                    "Traktor" or
                    "Snøskuter" or
                    "Lastebil" or
                    "Båt",

            BehovType.Evakuering =>
                ressursType is
                    "Mannskap/frivillige" or
                    "ATV" or
                    "Snøskuter" or
                    "Lastebil" or
                    "Båt" or
                    "Lokaler",

            // Behovstypen «Annet» kan matches med alle ressurstyper.
            BehovType.Annet => true,

            _ => false
        };
    }

    // Lager en forklaring på hvorfor ressursen matcher behovet.
    private static string LagMatchBegrunnelse(
        BehovType behovType,
        string ressursType)
    {
        if (behovType == BehovType.Annet)
        {
            return $"Behovet er av typen «Annet», så alle ressurstyper kan vurderes. Denne er en «{ressursType}».";
        }

        return $"Ressurstypen «{ressursType}» passer til behovstypen «{Visningstekst.Type(behovType)}».";
    }
}