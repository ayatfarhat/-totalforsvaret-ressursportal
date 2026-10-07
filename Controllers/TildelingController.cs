using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;

namespace Nabohjelp.Controllers;

public class TildelingController : Controller
{
    private readonly ApplicationDbContext _db;

    // Henter databasen gjennom dependency injection.
    public TildelingController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Tildeling
    // Viser behov som kan matches og tildeles.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var behov = await _db.Behovsliste
            .Where(b => b.Status != BehovStatus.Resolved)
            .OrderBy(b => b.Prioritet)
            .ThenBy(b => b.OpprettetTid)
            .ToListAsync();

        return View(behov);
    }

    // GET: /Tildeling/Match/5
    // Finner ressurser som passer til valgt behov.
    [HttpGet]
    public async Task<IActionResult> Match(int id)
    {
        var behov = await _db.Behovsliste.FindAsync(id);

        if (behov == null)
        {
            return NotFound();
        }

        // Henter ressursene som allerede er registrert.
        var ressurser = RessursController.HentRessurser();

        var matchendeRessurser = ressurser
            .Select((ressurs, index) => new
            {
                Ressurs = ressurs,
                Id = index
            })
            .Where(x =>
                x.Ressurs.Status == "Tilgjengelig" &&
                PasserTilBehov(behov.Type, x.Ressurs.Type))
            .Select(x => new MatchendeRessursViewModel
            {
                RessursId = x.Id,
                Type = x.Ressurs.Type,
                Posisjon = x.Ressurs.Posisjon,
                Status = x.Ressurs.Status,
                Navn = x.Ressurs.Navn,
                Beskrivelse = x.Ressurs.Beskrivelse,
                MatchBegrunnelse = LagMatchBegrunnelse(
                    behov.Type,
                    x.Ressurs.Type)
            })
            .ToList();

        // Samler behov og matcher i én ViewModel.
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
    // Tildeler valgt ressurs til behovet.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tildel(
        int behovId,
        int ressursId)
    {
        var behov = await _db.Behovsliste.FindAsync(behovId);

        if (behov == null)
        {
            return NotFound();
        }

        var ressurs = RessursController.HentRessurs(ressursId);

        if (ressurs == null)
        {
            return NotFound();
        }

        // Ressursen må fortsatt være tilgjengelig.
        if (ressurs.Status != "Tilgjengelig")
        {
            TempData["Feil"] = "Ressursen er ikke lenger tilgjengelig.";

            return RedirectToAction(
                nameof(Match),
                new { id = behovId });
        }

        // Kontrollerer matchen også på serveren.
        if (!PasserTilBehov(behov.Type, ressurs.Type))
        {
            TempData["Feil"] = "Ressursen passer ikke til dette behovet.";

            return RedirectToAction(
                nameof(Match),
                new { id = behovId });
        }

        // Marker ressursen som tildelt.
        ressurs.Status = "Tildelt";

        // Marker behovet som tildelt.
        behov.Status = BehovStatus.Assigned;
        behov.SistEndretTid = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Viser resultatet etter tildelingen.
        return RedirectToAction(
            nameof(Resultat),
            new
            {
                behovId,
                ressursId
            });
    }

    // GET: /Tildeling/Resultat
    // Viser resultatet av tildelingen.
    [HttpGet]
    public async Task<IActionResult> Resultat(
        int behovId,
        int ressursId)
    {
        var behov = await _db.Behovsliste.FindAsync(behovId);
        var ressurs = RessursController.HentRessurs(ressursId);

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
                    RessursId = ressursId,
                    Type = ressurs.Type,
                    Posisjon = ressurs.Posisjon,
                    Status = ressurs.Status,
                    Navn = ressurs.Navn,
                    Beskrivelse = ressurs.Beskrivelse,
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
                    "Båt",

            // Andre behov kan matches med ressursen "Annet".
            BehovType.Annet =>
                ressursType == "Annet",

            _ => false
        };
    }

    // Lager en enkel forklaring som vises til brukeren.
    private static string LagMatchBegrunnelse(
        BehovType behovType,
        string ressursType)
    {
        return $"Ressurstypen «{ressursType}» passer til behovstypen «{Visningstekst.Type(behovType)}».";
    }
}