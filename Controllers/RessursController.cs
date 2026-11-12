using Nabohjelp.Models;
using Nabohjelp.Services;
using Nabohjelp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.ViewModels;

namespace Nabohjelp.Controllers
{
    // Alle sidene krever innlogging. Utskriften krever i tillegg Admin eller Kommune.
    [Authorize]
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

        // «Ressurser og behov»: valg mellom å melde behov og registrere ressurs,
        // og lista over brukerens egne ressurser (også for Kommune og Admin).
        // Kriseledelsen ser alle ressursene i Ressurslisten (Oversikt under).
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            bool erKoordinator = Tilgang.ErAnsatt(User);
            string brukerId = Tilgang.BrukerId(User);

            // Egne ressurser, nyeste først (r => ... betyr «for hver ressurs r»).
            List<Ressurs> ressurser = await _db.Ressurser.AsNoTracking()
                .Where(r => r.OpprettetAvBrukerId == brukerId)
                .OrderByDescending(r => r.OpprettetTid)
                .ToListAsync();

            // Antall ønskede ressurser vises i en egen boks på siden.
            ViewBag.AntallOnskede = await _db.OnskedeRessurser.CountAsync();

            // Kriseledelsen ser også hvor mange behov som venter og hvor mange ressurser
            // som er ledige (bokser med lenke til behovskøen og ressurslisten).
            if (erKoordinator)
            {
                ViewBag.AntallAktiveBehov = await _db.Behovsliste
                    .Where(b => b.Status != BehovStatus.Resolved &&
                                b.Status != BehovStatus.Rejected &&
                                b.Status != BehovStatus.Withdrawn)
                    .CountAsync();

                ViewBag.AntallTilgjengeligeRessurser = await _db.Ressurser
                    .Where(r => r.Status == "Tilgjengelig")
                    .CountAsync();
            }

            return View(TilViewModeller(ressurser));
        }

        // GET: /Ressurs/Oversikt
        // Ressurslisten for kriseledelsen: alle ressurser, som behovskøen.
        // Kategori og filter kommer fra adressen (se ViewModels/RessursListeViewModel.cs).
        [HttpGet]
        [Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
        public async Task<IActionResult> Oversikt(RessursListeViewModel filter)
        {
            List<Ressurs> ressurser = await _db.Ressurser.AsNoTracking()
                .OrderByDescending(r => r.OpprettetTid)
                .ToListAsync();

            filter.Fyll(TilViewModeller(ressurser));
            return View(filter);
        }

        // Viser skjemaet for innloggede brukere.
        // Kommer brukeren fra lista over ønskede ressurser, er typen allerede valgt
        // (og teksten for «Annet», hvis kommunen skrev en).
        [HttpGet]
        public IActionResult Create(string? type, string? annet)
        {
            var skjema = new RessursFormViewModel();

            if (type != null && RessursFormViewModel.Typer.Contains(type))
            {
                skjema.Type = type;
                skjema.AnnetType = RessursFormViewModel.AnnetTypeForLagring(type, annet);
            }

            return View(skjema);
        }

        // Lagrer en ressurs med innlogget bruker som eier.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(Sikkerhet.NyRegistrering)] // maks 20 per minutt per bruker (se Services/Sikkerhet.cs)
        public async Task<IActionResult> Create(
            RessursFormViewModel model,
            IFormFile? bildefil)
        {
            // Skjult felt fylt ut = robot (se Services/Sikkerhet.cs).
            if (Sikkerhet.ErRobot(Request))
            {
                return BadRequest();
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
                OpprettetAvBrukerId = Tilgang.BrukerId(User)
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
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            Ressurs? ressurs = await _db.Ressurser.FindAsync(id);

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

        // GET: /Ressurs/Edit/5
        // Eieren, Kommune og Admin kan endre en ressurs.
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            Ressurs? ressurs = await _db.Ressurser.FindAsync(id);

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            RessursFormViewModel model,
            IFormFile? bildefil)
        {
            Ressurs? ressurs = await _db.Ressurser.FindAsync(id);

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

            TempData["Melding"] = Oversetter.Tekst("Endringene er lagret.");
            return RedirectToAction(nameof(Index));
        }

        // GET: /Ressurs/Delete/5
        // Viser hva som slettes, og ber brukeren bekrefte.
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            Ressurs? ressurs = await _db.Ressurser.FindAsync(id);

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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Ressurs? ressurs = await _db.Ressurser.FindAsync(id);

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

            TempData["Melding"] = Oversetter.Tekst("Ressursen er slettet.");
            return RedirectToAction(nameof(Index));
        }

        // GET: /Ressurs/Utskrift?fylke=Agder&kommune=4202
        // Ressurslisten per kommune, laget for å skrives ut eller lagres som PDF.
        // Kun kriseledelsen (Kommune og Admin) har tilgang.
        [Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
        [HttpGet]
        public async Task<IActionResult> Utskrift(string? fylke, string? kommune)
        {
            List<Ressurs> ressurser = await _db.Ressurser.AsNoTracking().ToListAsync();

            RessursUtskriftViewModel modell = RessursUtskriftViewModel.Lag(TilViewModeller(ressurser), fylke, kommune, DateTime.Now);

            return View(modell);
        }

        // Eieren av ressursen og kriseledelsen (Kommune og Admin) kan endre og slette.
        private bool KanEndre(Ressurs ressurs)
        {
            return Tilgang.KanEndre(User, ressurs.OpprettetAvBrukerId);
        }

        // Gir en forklaring hvis ressursen ikke kan slettes, ellers null.
        private async Task<string?> GrunnTilAtDenIkkeKanSlettes(Ressurs ressurs)
        {
            if (ressurs.Status == "Tildelt")
            {
                return Oversetter.Tekst("Ressursen er tildelt et behov akkurat nå og kan ikke slettes. Kontakt kommunen hvis den ikke lenger er tilgjengelig.");
            }

            // Tildelinger viser til ressursen (DeleteBehavior.Restrict i ApplicationDbContext),
            // så en ressurs som har vært brukt, blir liggende i historikken.
            bool harVaertTildelt = await _db.Tildelinger
                .AnyAsync(t => t.RessursId == ressurs.Id);

            if (harVaertTildelt)
            {
                return Oversetter.Tekst("Ressursen har vært brukt i en tildeling og må bli liggende i historikken. Du kan fortsatt endre den.");
            }

            return null;
        }

        // Felles kontroll for registrering og redigering.
        private void SjekkSkjema(RessursFormViewModel model, IFormFile? bildefil, bool sjekkTidspunkt)
        {
            // Typen må være en av de faste typene (ikke bare «en tekst»).
            if (!RessursFormViewModel.Typer.Contains(model.Type))
            {
                ModelState.AddModelError("Type", Oversetter.Tekst("Du må velge type ressurs."));
            }

            // Velger brukeren «Annet», må hen skrive hva det er.
            if (model.Type == "Annet" && string.IsNullOrWhiteSpace(model.AnnetType))
            {
                ModelState.AddModelError("AnnetType", Oversetter.Tekst("Skriv hva slags ressurs det er."));
            }

            // Kommunen brukeren skrev, må finnes i listen over norske kommuner.
            // Den lagres som kommunenummer.
            Kommune? kommune = Kommuner.FinnFraTekst(model.KommuneNavn);
            if (kommune != null)
            {
                model.Kommunenummer = kommune.Nummer;
            }
            else
            {
                model.Kommunenummer = null;
                if (!string.IsNullOrWhiteSpace(model.KommuneNavn))
                {
                    ModelState.AddModelError("KommuneNavn", Oversetter.Tekst("Fant ikke kommunen. Velg en kommune fra listen."));
                }
            }

            // Landskoden må være en av de som finnes i listen.
            if (!Land.KodeFinnes(model.Landskode))
            {
                ModelState.AddModelError("Landskode", Oversetter.Tekst("Velg en landskode fra listen."));
            }

            // Sjekker bildet (valgfritt) før noe lagres.
            if (bildefil != null && bildefil.Length > 0)
            {
                string? bildeFeil = Ressursbilder.Sjekk(bildefil);
                if (bildeFeil != null)
                {
                    ModelState.AddModelError("Bilde", Oversetter.Tekst(bildeFeil));
                }
            }

            // Kontrollerer tidspunktet for tilgjengelighet.
            if (sjekkTidspunkt && !model.ErValidTilgjengeligFra())
            {
                ModelState.AddModelError(
                    "TilgjengeligFra",
                    Oversetter.Tekst("Tidspunkt kan ikke ligge i fortiden."));
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

        private List<RessursFormViewModel> TilViewModeller(List<Ressurs> ressurser)
        {
            var modeller = new List<RessursFormViewModel>();
            foreach (Ressurs ressurs in ressurser)
            {
                modeller.Add(TilViewModel(ressurs));
            }
            return modeller;
        }
    }
}
