using Nabohjelp.Models;
using Nabohjelp.Services;
using Nabohjelp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Nabohjelp.ViewModels;

namespace Nabohjelp.Controllers
{
    public class RessursController : Controller
    {
        private readonly ApplicationDbContext _db;

        // Gir oss mappen wwwroot, der opplastede bilder lagres (se Services/Ressursbilder.cs).
        private readonly IWebHostEnvironment _miljo;

        // Gir tilgang til databasen og til mappen for bilder.
        public RessursController(ApplicationDbContext db, IWebHostEnvironment miljo)
        {
            _db = db;
            _miljo = miljo;
        }

        // Frivillige ser egne ressurser.
        // Admin og Kommune ser alle.
        [Authorize(Roles = "Admin,Kommune,Frivillig")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var sporring = _db.Ressurser.AsNoTracking();

            bool erKoordinator = Tilgang.ErAnsatt(User);

            if (!erKoordinator)
            {
                var brukerId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(brukerId))
                {
                    return Challenge();
                }

                // Filtrerer i databasen før data hentes.
                sporring = sporring.Where(
                    r => r.OpprettetAvBrukerId == brukerId);
            }

            var ressurser = await sporring
                .OrderByDescending(r => r.OpprettetTid)
                .ToListAsync();

            var modeller = ressurser.Select(TilViewModel).ToList();

            // Antall ønskede ressurser vises i en egen boks på siden.
            ViewBag.AntallOnskede = await _db.OnskedeRessurser.CountAsync();
            ViewBag.ErKoordinator = erKoordinator;

            return View(modeller);
        }

        // Viser skjemaet for innloggede brukere.
        // Kommer brukeren fra lista over ønskede ressurser, er typen allerede valgt.
        [Authorize]
        [HttpGet]
        public IActionResult Create(string? type)
        {
            var skjema = new RessursFormViewModel();

            if (type != null && RessursFormViewModel.Typer.Contains(type))
            {
                skjema.Type = type;
            }

            return View(skjema);
        }

        // Lagrer en ressurs med innlogget bruker som eier.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            RessursFormViewModel model,
            IFormFile? bildefil)
        {
            var brukerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(brukerId))
            {
                return Challenge();
            }

            // Bilde-adressen settes bare av oss, aldri av det som kommer fra skjemaet.
            model.BildeSti = null;

            SjekkSkjema(model, bildefil, sjekkTidspunkt: true);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Status, tidspunkt og eier settes her. Resten kommer fra skjemaet.
            var ressurs = new Ressurs
            {
                Status = "Tilgjengelig",
                OpprettetTid = DateTime.UtcNow,
                OpprettetAvBrukerId = brukerId
            };
            model.KopierTil(ressurs);

            // Databasen genererer ressursens ID.
            _db.Ressurser.Add(ressurs);
            await _db.SaveChangesAsync();

            // Bildet lagres med ressursens ID som filnavn, så det må skje etter lagringen.
            if (bildefil != null && bildefil.Length > 0)
            {
                Ressursbilder.Lagre(_miljo.WebRootPath, bildefil, ressurs.Id);
            }

            return RedirectToAction(
                nameof(Confirmation),
                new { id = ressurs.Id });
        }

        // Bare eieren, Admin og Kommune får se kvitteringen.
        [Authorize(Roles = "Admin,Kommune,Frivillig")]
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var ressurs = await _db.Ressurser
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ressurs == null)
            {
                return NotFound();
            }

            var brukerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            bool erEier =
                !string.IsNullOrEmpty(brukerId) &&
                ressurs.OpprettetAvBrukerId == brukerId;

            bool erKoordinator = Tilgang.ErAnsatt(User);

            if (!erEier && !erKoordinator)
            {
                return Forbid();
            }

            return View(TilViewModel(ressurs));
        }

        // GET: /Ressurs/Edit/5
        // Eieren, Kommune og Admin kan endre en ressurs.
        [Authorize(Roles = "Admin,Kommune,Frivillig")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ressurs = await _db.Ressurser
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ressurs == null)
            {
                return NotFound();
            }

            if (!KanEndre(ressurs))
            {
                return Forbid();
            }

            return View(TilViewModel(ressurs));
        }

        // POST: /Ressurs/Edit/5
        // Bare feltene fra skjemaet endres. Status og eier settes aldri fra skjemaet.
        [Authorize(Roles = "Admin,Kommune,Frivillig")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            RessursFormViewModel model,
            IFormFile? bildefil)
        {
            var ressurs = await _db.Ressurser
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ressurs == null)
            {
                return NotFound();
            }

            if (!KanEndre(ressurs))
            {
                return Forbid();
            }

            // Et tidspunkt som allerede var lagret, kan bli liggende selv om
            // det nå er passert. Bare et nytt tidspunkt må ligge fram i tid.
            bool nyttTidspunkt = !SammeMinutt(model.TilgjengeligFra, ressurs.TilgjengeligFra);
            SjekkSkjema(model, bildefil, sjekkTidspunkt: nyttTidspunkt);

            if (!ModelState.IsValid)
            {
                // Viser skjemaet på nytt med det som ligger i databasen
                // for feltene brukeren ikke kan endre.
                model.Id = ressurs.Id;
                model.Status = ressurs.Status;
                model.BildeSti = Ressursbilder.Finn(_miljo.WebRootPath, ressurs.Id);
                return View(model);
            }

            model.KopierTil(ressurs);
            await _db.SaveChangesAsync();

            // Nytt bilde erstatter det gamle. Uten nytt bilde kan det gamle fjernes.
            if (bildefil != null && bildefil.Length > 0)
            {
                Ressursbilder.Slett(_miljo.WebRootPath, ressurs.Id);
                Ressursbilder.Lagre(_miljo.WebRootPath, bildefil, ressurs.Id);
            }
            else if (model.FjernBilde)
            {
                Ressursbilder.Slett(_miljo.WebRootPath, ressurs.Id);
            }

            TempData["Melding"] = "Endringene er lagret.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Ressurs/Delete/5
        // Viser hva som slettes, og ber brukeren bekrefte.
        [Authorize(Roles = "Admin,Kommune,Frivillig")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var ressurs = await _db.Ressurser
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ressurs == null)
            {
                return NotFound();
            }

            if (!KanEndre(ressurs))
            {
                return Forbid();
            }

            // Hvis ressursen ikke kan slettes, forklarer siden hvorfor.
            ViewBag.Sperre = await GrunnTilAtDenIkkeKanSlettes(ressurs);

            return View(TilViewModel(ressurs));
        }

        // POST: /Ressurs/Delete/5
        [Authorize(Roles = "Admin,Kommune,Frivillig")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ressurs = await _db.Ressurser
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ressurs == null)
            {
                return NotFound();
            }

            if (!KanEndre(ressurs))
            {
                return Forbid();
            }

            // Sjekkes på nytt her, i tilfelle ressursen ble tildelt
            // mens bekreftelsessiden var åpen.
            string? sperre = await GrunnTilAtDenIkkeKanSlettes(ressurs);
            if (sperre != null)
            {
                TempData["Feil"] = sperre;
                return RedirectToAction(nameof(Index));
            }

            _db.Ressurser.Remove(ressurs);
            await _db.SaveChangesAsync();

            Ressursbilder.Slett(_miljo.WebRootPath, id);

            TempData["Melding"] = "Ressursen er slettet.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Ressurs/Utskrift?fylke=Agder&kommune=4202
        // Ressurslisten per kommune, laget for å skrives ut eller lagres som PDF.
        // Kun kriseledelsen (Kommune og Admin) har tilgang.
        [Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
        [HttpGet]
        public async Task<IActionResult> Utskrift(string? fylke, string? kommune)
        {
            var ressurser = await _db.Ressurser
                .AsNoTracking()
                .ToListAsync();

            var modell = RessursUtskriftViewModel.Lag(
                ressurser.Select(TilViewModel),
                fylke,
                kommune,
                DateTime.Now);

            return View(modell);
        }

        // Eieren av ressursen og kriseledelsen (Kommune og Admin) kan endre og slette.
        private bool KanEndre(Ressurs ressurs)
        {
            if (Tilgang.ErAnsatt(User))
            {
                return true;
            }

            var brukerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            return !string.IsNullOrEmpty(brukerId) &&
                   ressurs.OpprettetAvBrukerId == brukerId;
        }

        // Gir en forklaring hvis ressursen ikke kan slettes, ellers null.
        private async Task<string?> GrunnTilAtDenIkkeKanSlettes(Ressurs ressurs)
        {
            if (ressurs.Status == "Tildelt")
            {
                return "Ressursen er tildelt et behov akkurat nå og kan ikke slettes. " +
                       "Kontakt kommunen hvis den ikke lenger er tilgjengelig.";
            }

            // Tildelinger viser til ressursen (DeleteBehavior.Restrict i ApplicationDbContext),
            // så en ressurs som har vært brukt, blir liggende i historikken.
            bool harVaertTildelt = await _db.Tildelinger
                .AnyAsync(t => t.RessursId == ressurs.Id);

            if (harVaertTildelt)
            {
                return "Ressursen har vært brukt i en tildeling og må bli liggende i historikken. " +
                       "Du kan fortsatt endre den.";
            }

            return null;
        }

        // Felles kontroll for registrering og redigering.
        private void SjekkSkjema(RessursFormViewModel model, IFormFile? bildefil, bool sjekkTidspunkt)
        {
            // Typen må være en av de faste typene (ikke bare «en tekst»).
            if (!RessursFormViewModel.Typer.Contains(model.Type))
            {
                ModelState.AddModelError("Type", "Du må velge type ressurs.");
            }

            // Kommunen må finnes i listen over norske kommuner.
            if (!string.IsNullOrEmpty(model.Kommunenummer) && !Kommuner.Finnes(model.Kommunenummer))
            {
                ModelState.AddModelError("Kommunenummer", "Velg en kommune fra listen.");
            }

            // Landskoden må være en av de som finnes i listen.
            if (!RessursFormViewModel.Landskoder.Any(l => l.Kode == model.Landskode))
            {
                ModelState.AddModelError("Landskode", "Velg en landskode fra listen.");
            }

            // Sjekker bildet (valgfritt) før noe lagres.
            if (bildefil != null && bildefil.Length > 0)
            {
                string? bildeFeil = Ressursbilder.Sjekk(bildefil);
                if (bildeFeil != null)
                {
                    ModelState.AddModelError("Bilde", bildeFeil);
                }
            }

            // Kontrollerer tidspunktet for tilgjengelighet.
            if (sjekkTidspunkt && !model.ErValidTilgjengeligFra())
            {
                ModelState.AddModelError(
                    "TilgjengeligFra",
                    "Tidspunkt kan ikke ligge i fortiden.");
            }
        }

        // Skjemaet sender tidspunkt uten sekunder, så vi sammenligner til nærmeste minutt.
        private static bool SammeMinutt(DateTime? a, DateTime? b)
        {
            if (a == null || b == null)
            {
                return a == b;
            }

            return Math.Abs((a.Value - b.Value).TotalMinutes) < 1;
        }

        // Gjør en ressurs fra databasen klar for visningene, med bildet hvis det finnes.
        private RessursFormViewModel TilViewModel(Ressurs ressurs)
        {
            string? bildeSti = Ressursbilder.Finn(_miljo.WebRootPath, ressurs.Id);
            return RessursFormViewModel.FraRessurs(ressurs, bildeSti);
        }
    }
}
