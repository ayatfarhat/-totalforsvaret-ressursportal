using Nabohjelp.Models;
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

        // Gir oss mappen wwwroot, der opplastede bilder lagres.
        private readonly IWebHostEnvironment _miljo;

        // Regler for bildene som kan lastes opp.
        private static readonly string[] _tillatteBildeendelser = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaksBildeStorrelse = 5 * 1024 * 1024; // 5 MB

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

            bool erKoordinator =
                User.IsInRole("Admin") ||
                User.IsInRole("Kommune");

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

            // Kopierer skjemadata til databasemodellen.
            // Landskoden lagres sammen med nummeret, for eksempel «+47 12345678».
            var ressurs = new Ressurs
            {
                Type = model.Type,
                Posisjon = model.Posisjon,
                Kommunenummer = model.Kommunenummer,
                Breddegrad = model.Breddegrad,
                Lengdegrad = model.Lengdegrad,
                TilgjengeligFra = model.TilgjengeligFra,
                Beskrivelse = model.Beskrivelse,
                Navn = model.Navn,
                Telefon = model.TelefonForLagring(),
                Status = "Tilgjengelig",
                OpprettetTid = DateTime.UtcNow,
                OpprettetAvBrukerId = brukerId
            };

            // Databasen genererer ressursens ID.
            _db.Ressurser.Add(ressurs);
            await _db.SaveChangesAsync();

            // Bildet lagres med ressursens ID som filnavn (for eksempel 12.jpg),
            // så vi trenger ingen egen kolonne i databasen for det.
            if (bildefil != null && bildefil.Length > 0)
            {
                LagreBilde(bildefil, ressurs.Id);
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

            bool erKoordinator =
                User.IsInRole("Admin") ||
                User.IsInRole("Kommune");

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
                model.BildeSti = FinnBilde(ressurs.Id);
                return View(model);
            }

            ressurs.Type = model.Type;
            ressurs.Posisjon = model.Posisjon;
            ressurs.Kommunenummer = model.Kommunenummer;
            ressurs.Breddegrad = model.Breddegrad;
            ressurs.Lengdegrad = model.Lengdegrad;
            ressurs.TilgjengeligFra = model.TilgjengeligFra;
            ressurs.Beskrivelse = model.Beskrivelse;
            ressurs.Navn = model.Navn;
            ressurs.Telefon = model.TelefonForLagring();

            await _db.SaveChangesAsync();

            // Nytt bilde erstatter det gamle. Uten nytt bilde kan det gamle fjernes.
            if (bildefil != null && bildefil.Length > 0)
            {
                SlettBilde(ressurs.Id);
                LagreBilde(bildefil, ressurs.Id);
            }
            else if (model.FjernBilde)
            {
                SlettBilde(ressurs.Id);
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

            SlettBilde(id);

            TempData["Melding"] = "Ressursen er slettet.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Ressurs/Utskrift?fylke=Agder&kommune=4202
        // Ressurslisten per kommune, laget for å skrives ut eller lagres som PDF.
        // Kun kriseledelsen (Kommune og Admin) har tilgang.
        [Authorize(Roles = "Admin,Kommune")]
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
            if (User.IsInRole("Admin") || User.IsInRole("Kommune"))
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
                string? bildeFeil = SjekkBilde(bildefil);
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

        // Gjør databasedata klare for visningene.
        private RessursFormViewModel TilViewModel(
            Ressurs ressurs)
        {
            var modell = new RessursFormViewModel
            {
                Id = ressurs.Id,
                OpprettetAvBrukerId = ressurs.OpprettetAvBrukerId,
                Type = ressurs.Type,
                Posisjon = ressurs.Posisjon,
                Kommunenummer = ressurs.Kommunenummer,
                Breddegrad = ressurs.Breddegrad,
                Lengdegrad = ressurs.Lengdegrad,
                TilgjengeligFra = ressurs.TilgjengeligFra,
                Beskrivelse = ressurs.Beskrivelse,
                Navn = ressurs.Navn,
                Status = ressurs.Status,
                BildeSti = FinnBilde(ressurs.Id)
            };

            // Telefonen er lagret som «+47 12345678». Vi deler den opp igjen
            // i landskode og nummer, slik at visningen blir riktig.
            modell.LesTelefonFraLagring(ressurs.Telefon);

            return modell;
        }

        // Sjekker at filen er et bilde vi tillater. Gir en feilmelding, eller null hvis OK.
        private static string? SjekkBilde(IFormFile fil)
        {
            if (fil.Length > MaksBildeStorrelse)
            {
                return "Bildet er for stort. Maks størrelse er 5 MB.";
            }

            string endelse = Path.GetExtension(fil.FileName).ToLowerInvariant();
            if (!_tillatteBildeendelser.Contains(endelse))
            {
                return "Bildet må være JPG, PNG eller WebP.";
            }

            if (!fil.ContentType.StartsWith("image/"))
            {
                return "Filen er ikke et bilde.";
            }

            // Filtypen og Content-Type kommer fra nettleseren og kan være falske.
            // Derfor ser vi også på de første bytene i selve filen.
            if (!ErEkteBilde(fil, endelse))
            {
                return "Filen ser ikke ut som et ekte bilde.";
            }

            return null;
        }

        // Sjekker «signaturen» (de første bytene) i filen:
        //   JPEG begynner med FF D8 FF, PNG med 89 50 4E 47,
        //   WebP med «RIFF» og så «WEBP» fra byte 8.
        private static bool ErEkteBilde(IFormFile fil, string endelse)
        {
            byte[] start = new byte[12];
            int lest;
            using (var strom = fil.OpenReadStream())
            {
                lest = strom.Read(start, 0, start.Length);
            }

            if (lest < 12)
            {
                return false;
            }

            if (endelse == ".jpg" || endelse == ".jpeg")
            {
                return start[0] == 0xFF && start[1] == 0xD8 && start[2] == 0xFF;
            }

            if (endelse == ".png")
            {
                return start[0] == 0x89 && start[1] == 0x50 && start[2] == 0x4E && start[3] == 0x47;
            }

            if (endelse == ".webp")
            {
                return start[0] == 'R' && start[1] == 'I' && start[2] == 'F' && start[3] == 'F'
                    && start[8] == 'W' && start[9] == 'E' && start[10] == 'B' && start[11] == 'P';
            }

            return false;
        }

        // Mappen der bildene ligger: wwwroot/uploads/ressurser
        private string BildeMappe()
        {
            return Path.Combine(_miljo.WebRootPath, "uploads", "ressurser");
        }

        // Lagrer bildet som «ressurs-ID + filendelse», for eksempel 12.jpg.
        // Vi bruker ikke filnavnet brukeren sendte, slik at ingen kan
        // overskrive andre filer eller lage rare filnavn.
        private void LagreBilde(IFormFile fil, int ressursId)
        {
            string endelse = Path.GetExtension(fil.FileName).ToLowerInvariant();
            if (endelse == ".jpeg")
            {
                endelse = ".jpg";
            }

            string mappe = BildeMappe();
            Directory.CreateDirectory(mappe);

            string fullSti = Path.Combine(mappe, ressursId + endelse);
            using (var strom = new FileStream(fullSti, FileMode.Create))
            {
                fil.CopyTo(strom);
            }
        }

        // Finner bildet til en ressurs, hvis det finnes. Gir nettadressen
        // (for eksempel /uploads/ressurser/12.jpg), eller null.
        private string? FinnBilde(int ressursId)
        {
            string mappe = BildeMappe();
            foreach (string endelse in new[] { ".jpg", ".png", ".webp" })
            {
                if (System.IO.File.Exists(Path.Combine(mappe, ressursId + endelse)))
                {
                    return "/uploads/ressurser/" + ressursId + endelse;
                }
            }

            return null;
        }

        // Sletter bildet til en ressurs (alle filendelser), hvis det finnes.
        private void SlettBilde(int ressursId)
        {
            string mappe = BildeMappe();
            foreach (string endelse in new[] { ".jpg", ".png", ".webp" })
            {
                string sti = Path.Combine(mappe, ressursId + endelse);
                if (System.IO.File.Exists(sti))
                {
                    System.IO.File.Delete(sti);
                }
            }
        }
    }
}
