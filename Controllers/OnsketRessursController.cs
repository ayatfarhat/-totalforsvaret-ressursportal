using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;

namespace Nabohjelp.Controllers
{
    // Ønskede ressurser: kommunen og kriseledelsen registrerer hva de trenger.
    // Alle kan se listen, men bare Admin og Kommune kan legge til og fjerne.
    // Ønskene lagres i databasen (tabellen OnskedeRessurser), så de blir
    // ikke borte når appen starter på nytt.
    public class OnsketRessursController : Controller
    {
        private readonly ApplicationDbContext _db;

        // Gir tilgang til databasen.
        public OnsketRessursController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /OnsketRessurs
        // De nyeste ønskene vises først.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var onskede = await _db.OnskedeRessurser
                .AsNoTracking()
                .OrderByDescending(o => o.Opprettet)
                .ToListAsync();

            return View(onskede);
        }

        // GET: /OnsketRessurs/Create
        [Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
        [HttpGet]
        public IActionResult Create()
        {
            return View(new OnsketRessurs());
        }

        // POST: /OnsketRessurs/Create
        [Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OnsketRessurs model)
        {
            // Typen må være en av de vanlige ressurstypene.
            if (!RessursFormViewModel.Typer.Contains(model.Type))
            {
                ModelState.AddModelError("Type", "Du må velge type ressurs.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Id og tidspunkt settes av oss, ikke av skjemaet.
            var onsket = new OnsketRessurs
            {
                Type = model.Type,
                Antall = model.Antall,
                Sted = model.Sted,
                Beskrivelse = model.Beskrivelse,
                Opprettet = DateTime.UtcNow
            };

            _db.OnskedeRessurser.Add(onsket);
            await _db.SaveChangesAsync();

            TempData["Melding"] = "Ønsket ressurs er lagt til.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /OnsketRessurs/Fjern/3
        [Authorize(Policy = "Ansatt")] // Admin og Kommune (se Program.cs)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fjern(int id)
        {
            var onsket = await _db.OnskedeRessurser.FindAsync(id);

            // Er den allerede fjernet (for eksempel av en kollega), gjør vi ingenting.
            if (onsket != null)
            {
                _db.OnskedeRessurser.Remove(onsket);
                await _db.SaveChangesAsync();
            }

            TempData["Melding"] = "Ønsket ressurs er fjernet.";
            return RedirectToAction(nameof(Index));
        }
    }
}
