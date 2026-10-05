using Nabohjelp.Models;
using Microsoft.AspNetCore.Mvc;

namespace Nabohjelp.Controllers
{
    public class RessursController : Controller
    {
        // Midlertidig lagring av ressurser i minnet.
        // Skal senere erstattes med EF Core/database.
        private static readonly List<RessursFormViewModel> _midlertidigListe = new();

        // Brukes for å låse listen når vi legger til nye ressurser.
        private static readonly object _laas = new();

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

            // Låser listen mens vi legger til, slik at to innsendinger
            // samtidig ikke får samme id.
            int nyId;
            lock (_laas)
            {
                _midlertidigListe.Add(model);
                nyId = _midlertidigListe.Count - 1;
            }

            // Husker at denne nettleseren har sendt inn ressursen,
            // slik at bare innsenderen får se kvitteringen.
            HuskInnsendtId(nyId);

            // Sender brukeren til bekreftelsessiden.
            return RedirectToAction("Confirmation", new { id = nyId });
        }

        // GET: /Ressurs/Confirmation
        public IActionResult Confirmation(int id)
        {
            if (id < 0 || id >= _midlertidigListe.Count)
            {
                return RedirectToAction("Index");
            }

            // Kvitteringen viser navn, telefon og adresse.
            // Derfor får bare innsenderen, Admin og Kommune se den.
            bool erAdminEllerKommune =
                User.IsInRole("Admin") || User.IsInRole("Kommune");

            if (!erAdminEllerKommune && !ErSendtInnAvDenneNettleseren(id))
            {
                return RedirectToAction("Index");
            }

            return View(_midlertidigListe[id]);
        }

        // Sjekker om denne nettleseren selv har sendt inn ressursen.
        // Id-ene ligger i TempData som tekst, f.eks. "0,3,7".
        // Peek leser verdien uten å slette den, så kvitteringen
        // fortsatt vises hvis brukeren oppdaterer siden.
        private bool ErSendtInnAvDenneNettleseren(int id)
        {
            string? lagret = TempData.Peek("InnsendteRessursIder") as string;

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

        // Legger til en ny id i teksten med innsendte ressurser.
        // TempData lagres i en kryptert informasjonskapsel (cookie),
        // så brukeren kan ikke selv legge til andres id-er.
        private void HuskInnsendtId(int id)
        {
            string? lagret = TempData.Peek("InnsendteRessursIder") as string;

            if (string.IsNullOrEmpty(lagret))
            {
                TempData["InnsendteRessursIder"] = id.ToString();
            }
            else
            {
                TempData["InnsendteRessursIder"] = lagret + "," + id;
            }
        }
    }
}