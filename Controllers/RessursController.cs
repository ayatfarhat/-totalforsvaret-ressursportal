using Nabohjelp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Nabohjelp.Controllers
{
    public class RessursController : Controller
    {
        // Midlertidig lagring av ressurser i minnet.
        private static readonly List<RessursFormViewModel> _midlertidigListe = new();

        private static readonly object _laas = new();
        private static int _nesteId = 0;

        // Henter alle ressurser for intern bruk i systemet.
        public static IReadOnlyList<RessursFormViewModel> HentRessurser()
        {
            lock (_laas)
            {
                return _midlertidigListe.ToList().AsReadOnly();
            }
        }

        // Sletter alle ressurser registrert av en bestemt bruker.
        public static void SlettRessurserForBruker(string brukerId)
        {
            lock (_laas)
            {
                _midlertidigListe.RemoveAll(
                    ressurs => ressurs.OpprettetAvBrukerId == brukerId);
            }
        }

        // Henter en ressurs ved hjelp av dens faste ID.
        public static RessursFormViewModel? HentRessurs(int id)
        {
            lock (_laas)
            {
                return _midlertidigListe.FirstOrDefault(r => r.Id == id);
            }
        }

        // GET: /Ressurs
        // Frivillige ser bare egne ressurser.
        // Admin og Kommune ser alle ressurser.
        [Authorize(Roles = "Admin,Kommune,Frivillig")]
        [HttpGet]
        public IActionResult Index()
        {
           var ressurser = HentRessurser().ToList();

            if (User.IsInRole("Admin") || User.IsInRole("Kommune"))
{
    return View(ressurser.ToList());
}

            var brukerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(brukerId))
            {
                return Challenge();
            }

            var egneRessurser = ressurser
                .Where(r => r.OpprettetAvBrukerId == brukerId)
                .ToList();

            return View(egneRessurser);
        }

        // GET: /Ressurs/Create
        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new RessursFormViewModel());
        }

        // POST: /Ressurs/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RessursFormViewModel model)
        {
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

            model.Status = "Tilgjengelig";

            // Knytter ressursen til innlogget bruker.
            model.OpprettetAvBrukerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(model.OpprettetAvBrukerId))
            {
                return Challenge();
            }

            // Gir ressursen en unik ID og lagrer den.
            int nyId;

            lock (_laas)
            {
                nyId = _nesteId++;
                model.Id = nyId;
                _midlertidigListe.Add(model);
            }

            return RedirectToAction("Confirmation", new { id = nyId });
        }

        // GET: /Ressurs/Confirmation
        // Bare eieren, Admin og Kommune kan se bekreftelsen.
        [Authorize(Roles = "Admin,Kommune,Frivillig")]
        [HttpGet]
        public IActionResult Confirmation(int id)
        {
            var ressurs = HentRessurs(id);

            if (ressurs == null)
            {
                return NotFound();
            }

            bool erAdminEllerKommune =
                User.IsInRole("Admin") || User.IsInRole("Kommune");

            var brukerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!erAdminEllerKommune &&
                ressurs.OpprettetAvBrukerId != brukerId)
            {
                return Forbid();
            }

            return View(ressurs);
        }
    }
}