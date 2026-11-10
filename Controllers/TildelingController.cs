using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.Services;
using Nabohjelp.ViewModels;

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
        // Akutte først (Urgent = 0), deretter de eldste først.
        List<Behov> behov = await _db.Behovsliste
            .Where(b => b.Status != BehovStatus.Resolved &&
                        b.Status != BehovStatus.Rejected &&
                        b.Status != BehovStatus.Withdrawn)
            .OrderBy(b => b.Prioritet)
            .ThenBy(b => b.OpprettetTid)
            .ToListAsync();

        return View(behov);
    }

    // GET: /Tildeling/Match/5
    // Finner tilgjengelige ressurser som passer til behovet, naermeste først.
    [HttpGet]
    public async Task<IActionResult> Match(int id)
    {
        Behov? behov = await _db.Behovsliste.FindAsync(id);

        if (behov == null)
        {
            return NotFound();
        }

        // Et avsluttet behov (for eksempel via en gammel lenke) skal ikke vise tildel-knapper.
        if (BehovStatusflyt.ErAvsluttet(behov.Status))
        {
            TempData["Feil"] = Oversetter.Tekst("Behovet er «{0}» og kan ikke få flere ressurser.", Visningstekst.Status(behov.Status));
            return RedirectToAction(nameof(Index));
        }

        // Henter bare tilgjengelige ressurser fra databasen.
        List<Ressurs> ressurser = await _db.Ressurser
            .AsNoTracking()
            .Where(r => r.Status == "Tilgjengelig")
            .ToListAsync();

        // Beholder ressursene som passer til behovstypen.
        var matchende = new List<MatchendeRessursViewModel>();
        foreach (Ressurs ressurs in ressurser)
        {
            if (Ressursmatching.PasserTilBehov(behov.Type, ressurs.Type))
            {
                matchende.Add(LagMatchendeRessurs(behov, ressurs));
            }
        }

        // Nærmeste først (se SammenlignAvstand nederst).
        matchende.Sort(SammenlignAvstand);

        // Den nærmeste ressursen med kjent avstand er anbefalt.
        foreach (MatchendeRessursViewModel ressurs in matchende)
        {
            if (ressurs.AvstandKm != null)
            {
                ressurs.ErAnbefalt = true;
                break;
            }
        }

        return View(LagViewModel(behov, matchende));
    }

    // POST: /Tildeling/Tildel
    // Tildeler en ressurs og lagrer koblingen til behovet.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tildel(int behovId, int ressursId)
    {
        string brukerId = Tilgang.BrukerId(User);
        Behov? behov = await _db.Behovsliste.FindAsync(behovId);
        Ressurs? ressurs = await _db.Ressurser.FindAsync(ressursId);

        if (behov == null || ressurs == null)
        {
            return NotFound();
        }

        // Avsluttede behov kan ikke få nye tildelinger.
        if (BehovStatusflyt.ErAvsluttet(behov.Status))
        {
            TempData["Feil"] = Oversetter.Tekst("Behovet er «{0}» og kan ikke få flere ressurser.", Visningstekst.Status(behov.Status));

            return RedirectToAction(nameof(Index));
        }

        // Kontrollerer at ressurstypen passer til behovet.
        if (!Ressursmatching.PasserTilBehov(behov.Type, ressurs.Type))
        {
            TempData["Feil"] = Oversetter.Tekst("Ressursen passer ikke til dette behovet.");

            return RedirectToAction(
                nameof(Match),
                new { id = behovId });
        }

        // En transaksjon gjør at alle endringene under lagres samlet, eller ingen av dem.
        // «await using» lukker transaksjonen automatisk til slutt i metoden.
        await using var transaksjon = await _db.Database.BeginTransactionAsync();

        // Setter ressursen til «Tildelt» direkte i databasen, men bare hvis den
        // fortsatt er «Tilgjengelig». Hvis to fra kriseledelsen trykker «Tildel» på
        // samme ressurs samtidig, får bare den første det til (antallEndret blir 1).
        int antallEndret = await _db.Ressurser
            .Where(r => r.Id == ressursId && r.Status == "Tilgjengelig")
            .ExecuteUpdateAsync(oppdatering => oppdatering.SetProperty(r => r.Status, "Tildelt"));

        if (antallEndret == 0)
        {
            await transaksjon.RollbackAsync();

            TempData["Feil"] =
                Oversetter.Tekst("Ressursen er ikke lenger tilgjengelig.");

            return RedirectToAction(
                nameof(Match),
                new { id = behovId });
        }

        DateTime tidspunkt = DateTime.UtcNow;

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
        Behov? behov = await _db.Behovsliste.FindAsync(behovId);
        Ressurs? ressurs = await _db.Ressurser.FindAsync(ressursId);

        if (behov == null || ressurs == null)
        {
            return NotFound();
        }

        var tildelt = new List<MatchendeRessursViewModel>();
        tildelt.Add(LagMatchendeRessurs(behov, ressurs));
        return View(LagViewModel(behov, tildelt));
    }

    // Samler det sidene trenger å vise om behovet.
    private static TildelingViewModel LagViewModel(Behov behov, List<MatchendeRessursViewModel> ressurser)
    {
        return new TildelingViewModel
        {
            BehovId = behov.Id,
            BehovType = behov.Type,
            AnnetType = behov.AnnetType,
            Prioritet = behov.Prioritet,
            Status = behov.Status,
            Beskrivelse = behov.Beskrivelse,
            Latitude = behov.Latitude,
            Longitude = behov.Longitude,
            MatchendeRessurser = ressurser
        };
    }

    // Gjør én ressurs klar for visning, med avstand til behovet og forklaring.
    private static MatchendeRessursViewModel LagMatchendeRessurs(Behov behov, Ressurs ressurs)
    {
        return new MatchendeRessursViewModel
        {
            RessursId = ressurs.Id,
            Type = ressurs.Type,
            AnnetType = ressurs.AnnetType,
            Posisjon = ressurs.Posisjon,
            Status = ressurs.Status,
            Navn = ressurs.Navn,
            Beskrivelse = ressurs.Beskrivelse,
            AvstandKm = Avstandsberegning.BeregnAvstandKm(
                behov.Latitude, behov.Longitude,
                ressurs.Breddegrad, ressurs.Lengdegrad),
            MatchBegrunnelse = LagMatchBegrunnelse(behov.Type, ressurs.Type)
        };
    }

    // Bestemmer rekkefølgen i lista: kortest avstand først. Ressurser uten kjent
    // avstand (uten posisjon) kommer sist. Er avstanden lik, sorteres de på type.
    // Gir et negativt tall hvis a skal stå før b, og et positivt hvis b skal stå før a.
    private static int SammenlignAvstand(MatchendeRessursViewModel a, MatchendeRessursViewModel b)
    {
        double avstandA = double.MaxValue;
        if (a.AvstandKm != null)
        {
            avstandA = a.AvstandKm.Value;
        }

        double avstandB = double.MaxValue;
        if (b.AvstandKm != null)
        {
            avstandB = b.AvstandKm.Value;
        }

        if (avstandA != avstandB)
        {
            return avstandA.CompareTo(avstandB);
        }
        return string.Compare(a.Type, b.Type);
    }

    // Lager en forklaring på hvorfor ressursen matcher behovet.
    private static string LagMatchBegrunnelse(
        BehovType behovType,
        string ressursType)
    {
        if (behovType == BehovType.Annet)
        {
            return Oversetter.Tekst(
                "Behovet er av typen «Annet», så alle ressurstyper kan vurderes. Denne er en «{0}».",
                Oversetter.Tekst(ressursType));
        }

        return Oversetter.Tekst(
            "Ressurstypen «{0}» passer til behovstypen «{1}».",
            Oversetter.Tekst(ressursType),
            Visningstekst.Type(behovType));
    }
}