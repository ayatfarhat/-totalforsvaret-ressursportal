using Nabohjelp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nabohjelp.Controllers
{
    public class RessursController : Controller
    {
        // Midlertidig lagring av ressurser i minnet.
        private static readonly List<RessursFormViewModel> _midlertidigListe = new();

        private static readonly object _laas = new();
        private static int _nesteId = 0;

        // Gjør ressursene tilgjengelige for matching.
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
        public IActionResult Index()
        {
            return View(HentRessurser());
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
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(model.OpprettetAvBrukerId))
            {
                return Challenge();
            }

            // Gir ressursen en unik ID.
            int nyId;

            lock (_laas)
            {
                nyId = _nesteId++;
                model.Id = nyId;
                _midlertidigListe.Add(model);
            }

            HuskInnsendtId(nyId);

            return RedirectToAction("Confirmation", new { id = nyId });
        }

        // GET: /Ressurs/Confirmation
        public IActionResult Confirmation(int id)
        {
            var ressurs = HentRessurs(id);

            if (ressurs == null)
            {
                return RedirectToAction("Index");
            }

            bool erAdminEllerKommune =
                User.IsInRole("Admin") || User.IsInRole("Kommune");

            if (!erAdminEllerKommune &&
                !ErSendtInnAvDenneNettleseren(id))
            {
                return RedirectToAction("Index");
            }

            return View(ressurs);
        }

        // Kontrollerer om denne nettleseren har sendt inn ressursen.
        private bool ErSendtInnAvDenneNettleseren(int id)
        {
            string? lagret =
                TempData.Peek("InnsendteRessursIder") as string;

            if (string.IsNullOrEmpty(lagret))
            {
                return false;
            }

            string[] ider = lagret.Split(',');

            foreach (string lagretId in ider)
            {
                if (lagretId == id.ToString())
                {
                    return true;
                }
            }

            return false;
        }

        // Husker ressurs-ID for bekreftelsessiden.
        private void HuskInnsendtId(int id)
        {
            string? lagret =
                TempData.Peek("InnsendteRessursIder") as string;

            if (string.IsNullOrEmpty(lagret))
            {
                TempData["InnsendteRessursIder"] = id.ToString();
            }
            else
            {
                TempData["InnsendteRessursIder"] =
                    lagret + "," + id;
            }
        }
    }
}