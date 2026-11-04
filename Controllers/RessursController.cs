using Nabohjelp.Models;
using Microsoft.AspNetCore.Mvc;

namespace Nabohjelp.Controllers
{
    public class RessursController : Controller
    {
        // Gir oss mappen wwwroot, der opplastede bilder lagres.
        private readonly IWebHostEnvironment _miljo;

        public RessursController(IWebHostEnvironment miljo)
        {
            _miljo = miljo;
        }

        // Regler for bildene som kan lastes opp.
        private static readonly string[] _tillatteBildeendelser = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaksBildeStorrelse = 5 * 1024 * 1024; // 5 MB

        // Midlertidig lagring av ressurser i minnet.
        // Skal senere erstattes med EF Core/database.
        private static readonly List<RessursFormViewModel> _midlertidigListe = new();

        // Brukes for å låse listen når vi legger til nye ressurser.
        private static readonly object _laas = new();

        // Gjør ressursene tilgjengelige for matching.
        // Listen kan leses, men ikke erstattes utenfra.
        // Vi gir tilbake en kopi av listen. Da kan andre sider gå gjennom den
        // uten at en ny innsending samtidig endrer listen og gir feil
        // («Collection was modified»).
        public static IReadOnlyList<RessursFormViewModel> HentRessurser()
        {
            lock (_laas)
            {
                return _midlertidigListe.ToList();
            }
        }

        // Henter én bestemt ressurs.
        public static RessursFormViewModel? HentRessurs(int id)
        {
            lock (_laas)
            {
                if (id < 0 || id >= _midlertidigListe.Count)
                {
                    return null;
                }

                return _midlertidigListe[id];
            }
        }

        // Gjør en ressurs «Tildelt», men bare hvis den fortsatt er «Tilgjengelig».
        // Sjekken og endringen skjer i ett steg (under lås), slik at to personer
        // ikke kan tildele samme ressurs samtidig. Gir false hvis den allerede er tatt.
        public static bool ForsokTildel(int id)
        {
            lock (_laas)
            {
                if (id < 0 || id >= _midlertidigListe.Count)
                {
                    return false;
                }

                if (_midlertidigListe[id].Status != "Tilgjengelig")
                {
                    return false;
                }

                _midlertidigListe[id].Status = "Tildelt";
                return true;
            }
        }

        // Setter ressursen tilbake til «Tilgjengelig». Brukes hvis noe feiler
        // etter at ForsokTildel har gått gjennom.
        public static void Frigjor(int id)
        {
            lock (_laas)
            {
                if (id >= 0 && id < _midlertidigListe.Count)
                {
                    _midlertidigListe[id].Status = "Tilgjengelig";
                }
            }
        }

        // GET: /Ressurs
        public IActionResult Index()
        {
            ViewBag.AntallOnskede = OnsketRessursController.Antall();
            return View(HentRessurser().ToList());
        }

        // GET: /Ressurs/Create
        [HttpGet]
        public IActionResult Create(string? type)
        {
            var skjema = new RessursFormViewModel();

            // Kommer brukeren fra lista over ønskede ressurser, er typen allerede valgt.
            if (type != null && RessursFormViewModel.Typer.Contains(type))
            {
                skjema.Type = type;
            }

            return View(skjema);
        }

        // POST: /Ressurs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(RessursFormViewModel model, IFormFile? bildefil)
        {
            // Bilde-adressen settes bare av oss, aldri av det som kommer fra skjemaet.
            model.BildeSti = null;

            // Typen må være en av de faste typene (ikke bare «en tekst»).
            if (!RessursFormViewModel.Typer.Contains(model.Type))
            {
                ModelState.AddModelError("Type", "Du må velge type ressurs.");
            }

            // Landskoden må være en av de som finnes i listen.
            if (!RessursFormViewModel.Landskoder.Any(l => l.Kode == model.Landskode))
            {
                ModelState.AddModelError("Landskode", "Velg en landskode fra listen.");
            }

            // Sjekker bildet (valgfritt) før vi lagrer det.
            if (bildefil != null && bildefil.Length > 0)
            {
                string? bildeFeil = SjekkBilde(bildefil);
                if (bildeFeil != null)
                {
                    ModelState.AddModelError("Bilde", bildeFeil);
                }
            }

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

            // Alt er gyldig, så vi kan lagre bildet på disken.
            if (bildefil != null && bildefil.Length > 0)
            {
                model.BildeSti = LagreBilde(bildefil);
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
            RessursFormViewModel? ressurs = HentRessurs(id);
            if (ressurs == null)
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

            return View(ressurs);
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

        // Lagrer bildet i wwwroot/uploads/ressurser med et tilfeldig navn
        // og gir tilbake nettadressen til bildet.
        // Vi bruker ikke filnavnet brukeren sendte, slik at ingen kan
        // overskrive andre filer eller lage rare filnavn.
        private string LagreBilde(IFormFile fil)
        {
            string endelse = Path.GetExtension(fil.FileName).ToLowerInvariant();
            string nyttNavn = Guid.NewGuid().ToString("N") + endelse;

            string mappe = Path.Combine(_miljo.WebRootPath, "uploads", "ressurser");
            Directory.CreateDirectory(mappe);

            string fullSti = Path.Combine(mappe, nyttNavn);
            using (var strom = new FileStream(fullSti, FileMode.Create))
            {
                fil.CopyTo(strom);
            }

            return "/uploads/ressurser/" + nyttNavn;
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