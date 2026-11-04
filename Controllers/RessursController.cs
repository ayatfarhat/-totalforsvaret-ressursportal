using Nabohjelp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nabohjelp.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Nabohjelp.Controllers
{
    public class RessursController : Controller
    {
        private readonly ApplicationDbContext _db;

        // Gir controlleren tilgang til databasen.
        public RessursController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Viser ressurser fra databasen.
        public async Task<IActionResult> Index()
        {
            var ressurser = await _db.Ressurser
                .AsNoTracking()
                .OrderByDescending(r => r.OpprettetTid)
                .ToListAsync();

            var modeller = ressurser.Select(TilViewModel).ToList();

            return View(modeller);
        }

        // Viser skjemaet for innloggede brukere.
        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new RessursFormViewModel());
        }

        // Lagrer en ny ressurs i databasen.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            RessursFormViewModel model)
        {
            // Finner brukeren som registrerer ressursen.
            var brukerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(brukerId))
            {
                return Challenge();
            }

            // Kontrollerer tidspunktet for tilgjengelighet.
            if (!model.ErValidTilgjengeligFra())
            {
                ModelState.AddModelError(
                    "TilgjengeligFra",
                    "Tidspunkt kan ikke ligge i fortiden.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kopierer skjemadata til databasemodellen.
            var ressurs = new Ressurs
            {
                Type = model.Type,
                Posisjon = model.Posisjon,
                Breddegrad = model.Breddegrad,
                Lengdegrad = model.Lengdegrad,
                TilgjengeligFra = model.TilgjengeligFra,
                Beskrivelse = model.Beskrivelse,
                Navn = model.Navn,
                Telefon = model.Telefon,
                Status = "Tilgjengelig",
                OpprettetTid = DateTime.UtcNow,
                OpprettetAvBrukerId = brukerId
            };

            _db.Ressurser.Add(ressurs);
            await _db.SaveChangesAsync();

            // Husker tilgangen til kvitteringen.
            HuskInnsendtId(ressurs.Id);

            return RedirectToAction(
                nameof(Confirmation),
                new { id = ressurs.Id });
        }

        // Viser kvitteringen til innsender eller koordinator.
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

            if (!erEier &&
                !erKoordinator &&
                !ErSendtInnAvDenneNettleseren(id))
            {
                return RedirectToAction(nameof(Index));
            }

            return View(TilViewModel(ressurs));
        }

        // Gjør databasedata klare for de eksisterende visningene.
        private static RessursFormViewModel TilViewModel(
            Ressurs ressurs)
        {
            return new RessursFormViewModel
            {
                Id = ressurs.Id,
                OpprettetAvBrukerId = ressurs.OpprettetAvBrukerId,
                Type = ressurs.Type,
                Posisjon = ressurs.Posisjon,
                Breddegrad = ressurs.Breddegrad,
                Lengdegrad = ressurs.Lengdegrad,
                TilgjengeligFra = ressurs.TilgjengeligFra,
                Beskrivelse = ressurs.Beskrivelse,
                Navn = ressurs.Navn,
                Telefon = ressurs.Telefon,
                Status = ressurs.Status
            };
        }

        // Kontrollerer nettleserens tilgang til kvitteringen.
        private bool ErSendtInnAvDenneNettleseren(int id)
        {
            string? lagret =
                TempData.Peek("InnsendteDatabaseRessursIder")
                as string;

            if (string.IsNullOrEmpty(lagret))
            {
                return false;
            }

            return lagret.Split(',').Contains(id.ToString());
        }

        // Husker ID-en uten å blande inn gamle minneliste-ID-er.
        private void HuskInnsendtId(int id)
        {
            string? lagret =
                TempData.Peek("InnsendteDatabaseRessursIder")
                as string;

            if (string.IsNullOrEmpty(lagret))
            {
                TempData["InnsendteDatabaseRessursIder"] =
                    id.ToString();
            }
            else
            {
                TempData["InnsendteDatabaseRessursIder"] =
                    lagret + "," + id;
            }
        }
    }
}