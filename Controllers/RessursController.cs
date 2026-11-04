using Nabohjelp.Models;
using Nabohjelp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Nabohjelp.Controllers
{
    public class RessursController : Controller
    {
        private readonly ApplicationDbContext _db;

        // Gir tilgang til databasen.
        public RessursController(ApplicationDbContext db)
        {
            _db = db;
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

            return View(modeller);
        }

        // Viser skjemaet for innloggede brukere.
        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new RessursFormViewModel());
        }

        // Lagrer en ressurs med innlogget bruker som eier.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            RessursFormViewModel model)
        {
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

            // Databasen genererer ressursens ID.
            _db.Ressurser.Add(ressurs);
            await _db.SaveChangesAsync();

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

        // Gjør databasedata klare for visningene.
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
    }
}