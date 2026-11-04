using Nabohjelp.Models;
using Microsoft.AspNetCore.Mvc;
using Nabohjelp.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Nabohjelp.Controllers
{
    public class RessursController : Controller
    {
        private readonly ApplicationDbContext _db;

        public RessursController(ApplicationDbContext db)
        {
            _db = db;
        }


        public async Task<IActionResult> Index()
        {
            var ressurser = await _db.Ressurser
                .AsNoTracking()
                .OrderByDescending(r => r.OpprettetTid)
                .ToListAsync();

            var modeller = ressurser.Select(TilViewModel).ToList();

             return View(modeller);
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
public async Task<IActionResult> Create(RessursFormViewModel model)
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
        OpprettetAvBrukerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
    };

    _db.Ressurser.Add(ressurs);
    await _db.SaveChangesAsync();

    HuskInnsendtId(ressurs.Id);

    return RedirectToAction(
        nameof(Confirmation),
        new { id = ressurs.Id });
}

        // GET: /Ressurs/Confirmation
        public async Task<IActionResult> Confirmation(int id)
        {
            bool erAdminEllerKommune =
            User.IsInRole("Admin") || User.IsInRole("Kommune");

            if (!erAdminEllerKommune &&
            !ErSendtInnAvDenneNettleseren(id))
            {
            return RedirectToAction(nameof(Index));
            }

            var ressurs = await _db.Ressurser
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

            if (ressurs == null)
            {
            return NotFound();
            }

            return View(TilViewModel(ressurs));
        }

        private static RessursFormViewModel TilViewModel(Ressurs ressurs)
        {
            return new RessursFormViewModel
            {
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

        // Sjekker om denne nettleseren selv har sendt inn ressursen.
        // Id-ene ligger i TempData som tekst, f.eks. "0,3,7".
        // Peek leser verdien uten å slette den, så kvitteringen
        // fortsatt vises hvis brukeren oppdaterer siden.
        private bool ErSendtInnAvDenneNettleseren(int id)
        {
            string? lagret = TempData.Peek("InnsendteDatabaseRessursIder") as string;

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
            string? lagret = TempData.Peek("InnsendteDatabaseRessursIder") as string;

            if (string.IsNullOrEmpty(lagret))
            {
                TempData["InnsendteDatabaseRessursIder"] = id.ToString();
            }
            else
            {
                TempData["InnsendteDatabaseRessursIder"] = lagret + "," + id;
            }
        }
    }
}