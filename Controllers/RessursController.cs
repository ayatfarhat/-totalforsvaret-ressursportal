using Nabohjelp.Models;
using Microsoft.AspNetCore.Mvc;

namespace Nabohjelp.Controllers
{
    public class RessursController : Controller
    {
        // Midlertidig lagring av ressurser i minnet.
        // Skal senere erstattes med EF Core/database.
        private static readonly List<RessursFormViewModel> _midlertidigListe = new();

        // Gjør ressursene tilgjengelige for matching.
        // Listen kan leses, men ikke erstattes utenfra.
        public static IReadOnlyList<RessursFormViewModel> HentRessurser()
        {
            return _midlertidigListe.AsReadOnly();
        }

        // Henter én bestemt ressurs.
        public static RessursFormViewModel? HentRessurs(int id)
        {
            if (id < 0 || id >= _midlertidigListe.Count)
            {
                return null;
            }

            return _midlertidigListe[id];
        }

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
            // Kontrollerer at tidspunktet ikke ligger i fortiden.
            if (!model.ErValidTilgjengeligFra())
            {
                ModelState.AddModelError(
                    "TilgjengeligFra",
                    "Tidspunkt kan ikke ligge i fortiden.");
            }

            // Viser skjemaet igjen hvis dataene er ugyldige.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Ny ressurs blir tilgjengelig for matching.
            model.Status = "Tilgjengelig";

            _midlertidigListe.Add(model);

            // Sender brukeren til bekreftelsessiden.
            return RedirectToAction(
                "Confirmation",
                new { id = _midlertidigListe.Count - 1 });
        }

        // GET: /Ressurs/Confirmation
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