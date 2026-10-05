using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;

namespace Nabohjelp.Controllers
{
    // Bare Admin og Kommune (Hassans roller) kommer inn her. Alle andre blir
    // sendt til /Account/AccessDenied. Ikke logget inn i det hele tatt ->
    // sendt videre til innloggingssiden automatisk.
    // NB: Rollenavnene her ("Admin", "Kommune") er de Hassan faktisk har laget.
    // Oppgaveteksten nevner "kriseoperatør" som eget rollenavn - avklar med
    // Hassan/gruppa om det skal være en egen rolle, eller om "Kommune" er ment
    // å dekke det samme.
    [Authorize(Roles = "Admin,Kommune")]
    public class KommandobroController : Controller
    {
        private readonly ApplicationDbContext _db;

        public KommandobroController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Kartet vises direkte på Kommandobro-siden, så vi bygger
            // punktene her i stedet for på en egen kart-side.
            var modell = new MapViewModel();

            // Ressurser: hentes fra Marcelis midlertidige liste i minnet.
            // Bare de som faktisk har en posisjon valgt på kartet, vises.
            foreach (var ressurs in RessursController.HentRessurser())
            {
                if (ressurs.Breddegrad == null || ressurs.Lengdegrad == null)
                {
                    continue;
                }

                modell.Ressurser.Add(new KartPunkt
                {
                    Tittel = ressurs.Type,
                    Breddegrad = (double)ressurs.Breddegrad.Value,
                    Lengdegrad = (double)ressurs.Lengdegrad.Value
                });
            }

            // Behov: hentes fra databasen.
            var behovsliste = await _db.Behovsliste.ToListAsync();
            foreach (var behov in behovsliste)
            {
                modell.Behov.Add(new KartPunkt
                {
                    Tittel = Visningstekst.Type(behov.Type),
                    Breddegrad = behov.Latitude,
                    Lengdegrad = behov.Longitude
                });
            }

            return View(modell);
        }
    }
}
