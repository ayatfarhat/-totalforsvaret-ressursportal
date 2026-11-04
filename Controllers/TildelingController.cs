using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using System.Security.Claims;

namespace Nabohjelp.Controllers;

// Bare Admin og Kommune kan bruke matching og tildeling.
[Authorize(Roles = "Admin,Kommune")]
public class TildelingController : Controller
{
    private readonly ApplicationDbContext _db;

    // Gir controlleren tilgang til databasen.
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
        var ressurser = await _db.Ressurser
            .AsNoTracking()
            .Where(r => r.Status == "Tilgjengelig")
            .ToListAsync();

        // Velger tilgjengelige ressurser som passer til behovet.
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
                MatchBegrunnelse = LagMatchBegrunnelse(
                    behov.Type,
                    r.Type)
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
// Tildeler en ressurs og lagrer koblingen til behovet.
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Tildel(
    int behovId,
    int ressursId)
{
    // Henter ID-en til den innloggede brukeren.
    var brukerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(brukerId))
    {
        return Challenge();
    }

    // Henter behovet og ressursen fra databasen.
    var behov = await _db.Behovsliste.FindAsync(behovId);
    var ressurs = await _db.Ressurser.FindAsync(ressursId);

    if (behov == null || ressurs == null)
    {
        return NotFound();
    }

    // Hindrer nye tildelinger til et løst behov.
    if (behov.Status == BehovStatus.Resolved)
    {
        TempData["Feil"] = "Behovet er allerede løst.";
        return RedirectToAction(nameof(Index));
    }

    // Sjekker at ressurstypen passer til behovet.
    if (!PasserTilBehov(behov.Type, ressurs.Type))
    {
        TempData["Feil"] = "Ressursen passer ikke til dette behovet.";

        return RedirectToAction(
            nameof(Match),
            new { id = behovId });
    }

    // Sørger for at alle endringene lagres samlet.
    await using var transaksjon =
        await _db.Database.BeginTransactionAsync();

    // Tildeler bare hvis ressursen fortsatt er tilgjengelig.
    // Hindrer at to forespørsler tildeler samme ressurs samtidig.
    var antallEndret = await _db.Ressurser
        .Where(r => r.Id == ressursId &&
                    r.Status == "Tilgjengelig")
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(r => r.Status, "Tildelt"));

    if (antallEndret == 0)
    {
        await transaksjon.RollbackAsync();

        TempData["Feil"] = "Ressursen er ikke lenger tilgjengelig.";

        return RedirectToAction(
            nameof(Match),
            new { id = behovId });
    }

    var tidspunkt = DateTime.UtcNow;

    // Oppdaterer behovets status og siste endring.
    behov.Status = BehovStatus.Assigned;
    behov.SistEndretTid = tidspunkt;
    behov.SistEndretAvBrukerId = brukerId;

    // Kobler behov, ressurs og brukeren som tildelte.
    var tildeling = new Tildeling
    {
        BehovId = behovId,
        RessursId = ressursId,
        TildeltAvBrukerId = brukerId,
        TildeltTid = tidspunkt
    };

    _db.Tildelinger.Add(tildeling);

    // Lagrer behovet og den nye tildelingen.
    await _db.SaveChangesAsync();

    // Bekrefter alle endringene i transaksjonen.
    await transaksjon.CommitAsync();

    // Åpner resultatsiden.
    return RedirectToAction(
        nameof(Resultat),
        new { behovId, ressursId });
}

    // GET: /Tildeling/Resultat
    // Viser resultatet av tildelingen.
    [HttpGet]
    public async Task<IActionResult> Resultat(
        int behovId,
        int ressursId)
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