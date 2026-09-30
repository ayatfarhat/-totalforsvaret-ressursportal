using Kriseportal.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kriseportal.Controllers
{
    public class RessursController : Controller
    {
        // TODO: erstatt med EF Core når Torbjørns modell er klar
        // Midlertidig "database" i minnet — kun for å bygge og teste hele flyten
        private static readonly List<RessursFormViewModel> _midlertidigListe = new();

        // GET: /Ressurs
        public IActionResult Index()
        {
            return View(_midlertidigListe);
        }

        // GET: /Ressurs/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new RessursFormViewModel());
        }

        // POST: /Ressurs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RessursFormViewModel model)
        {
            // Type og Posisjon sjekkes allerede av [Required] i RessursFormViewModel.
            // Her sjekker vi bare det [Required] ikke kan uttrykke: at tidspunktet
            // ikke ligger i fortiden.
            if (!model.ErValidTilgjengeligFra())
            {
                ModelState.AddModelError("TilgjengeligFra", "Tidspunkt kan ikke ligge i fortiden.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Status settes automatisk, ikke av bruker
            model.Status = "Tilgjengelig";

            // TODO: erstatt med EF Core-lagring
            _midlertidigListe.Add(model);

            // Post-Redirect-Get: unngå dobbel innsending ved refresh
            return RedirectToAction("Confirmation", new { id = _midlertidigListe.Count - 1 });
        }

        // GET: /Ressurs/Confirmation/5
        public IActionResult Confirmation(int id)
        {
            if (id < 0 || id >= _midlertidigListe.Count)
            {
                return RedirectToAction("Index");
            }

            return View(_midlertidigListe[id]);
        }
    }
}
