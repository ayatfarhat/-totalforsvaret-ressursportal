using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nabohjelp.Models;

namespace Nabohjelp.Controllers
{
    // Ønskede ressurser: kommunen og kriseledelsen registrerer hva de trenger.
    // Alle kan se listen, men bare Admin og Kommune kan legge til og fjerne.
    public class OnsketRessursController : Controller
    {
        // Midlertidig lagring i minnet, på samme måte som i RessursController.
        // TODO: erstatt med EF Core når vi får en tabell for dette.
        private static readonly List<OnsketRessurs> _onskede = new();
        private static readonly object _laas = new();
        private static int _nesteId = 1;

        // Brukes av siden «Ressurser og behov» til å vise hvor mange som er ønsket.
        public static int Antall()
        {
            lock (_laas)
            {
                return _onskede.Count;
            }
        }

        // GET: /OnsketRessurs
        public IActionResult Index()
        {
            List<OnsketRessurs> kopi;
            lock (_laas)
            {
                kopi = _onskede.ToList();
            }
            return View(kopi);
        }

        // GET: /OnsketRessurs/Create
        [Authorize(Roles = "Admin,Kommune")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new OnsketRessurs());
        }

        // POST: /OnsketRessurs/Create
        [Authorize(Roles = "Admin,Kommune")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OnsketRessurs model)
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

            lock (_laas)
            {
                model.Id = _nesteId;
                _nesteId++;
                model.Opprettet = DateTime.Now;
                _onskede.Add(model);
            }

            TempData["Melding"] = "Ønsket ressurs er lagt til.";
            return RedirectToAction("Index");
        }

        // POST: /OnsketRessurs/Fjern/3
        [Authorize(Roles = "Admin,Kommune")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Fjern(int id)
        {
            lock (_laas)
            {
                _onskede.RemoveAll(o => o.Id == id);
            }

            TempData["Melding"] = "Ønsket ressurs er fjernet.";
            return RedirectToAction("Index");
        }
    }
}
