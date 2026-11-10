using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.Services;
using Nabohjelp.ViewModels;

namespace Nabohjelp.Controllers;

// Søk på nettsiden: /Sok?q=aggregat
// Alle kan søke i sidene og i «Spørsmål og svar». Innloggede får også treff i
// behov og ressurser: frivillige i sine egne, Admin og Kommune i alle.
// Selve søket ligger i Services/Nettstedsok.cs.
public class SokController : Controller
{
    private readonly ApplicationDbContext _db;

    public SokController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var resultat = new SokViewModel
        {
            ErInnlogget = Tilgang.ErInnlogget(User),
            ErAnsatt = Tilgang.ErAnsatt(User)
        };

        if (string.IsNullOrWhiteSpace(q))
        {
            return View(resultat);
        }

        // Et søk trenger ikke være langt. Vi kutter av, så ingen kan sende inn enorme tekster.
        string sok = q.Trim();
        if (sok.Length > 100)
        {
            sok = sok.Substring(0, 100);
        }
        resultat.Sok = sok;

        resultat.Sider = Nettstedsok.FinnSider(sok, resultat.ErAnsatt, Tilgang.ErAdmin(User));
        resultat.Sporsmal = Nettstedsok.FinnSporsmal(sok);

        if (resultat.ErInnlogget)
        {
            // Henter behovene og ressursene brukeren har lov til å se, og søker i dem.
            // Teksten sammenlignes på valgt språk, derfor søker vi etter at de er hentet.
            IQueryable<Ressurs> ressurser = _db.Ressurser.AsNoTracking();
            IQueryable<Behov> behov = _db.Behovsliste.AsNoTracking();

            if (!resultat.ErAnsatt)
            {
                string brukerId = Tilgang.BrukerId(User);
                ressurser = ressurser.Where(r => r.OpprettetAvBrukerId == brukerId);
                behov = behov.Where(b => b.OpprettetAvBrukerId == brukerId);
            }

            resultat.Ressurser = Nettstedsok.FinnRessurser(sok, await ressurser.OrderByDescending(r => r.OpprettetTid).ToListAsync());
            resultat.Behov = Nettstedsok.FinnBehov(sok, await behov.OrderByDescending(b => b.OpprettetTid).ToListAsync());
        }

        return View(resultat);
    }
}
