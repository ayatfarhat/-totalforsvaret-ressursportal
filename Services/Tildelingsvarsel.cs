using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;

namespace Nabohjelp.Services;

// Varsel til den som eier en ressurs, når ressursen blir tildelt et behov.
//
// Slik virker det:
//   1. TildelingController.Tildel lagrer tildelingen med EierHarSett = false.
//   2. Forsiden, «Mine ressurser» og brukermenyen spør HentNye og viser varselet.
//   3. Eieren trykker «Jeg har sett det». RessursController.SettVarsel setter EierHarSett = true.
//
// Nettsiden kan ikke sende e-post, så varselet vises bare på nettsiden.
// Eieren får vite hvilken ressurs og hvilken kommune, men ikke hvem som trenger hjelpen
// eller kontaktinfo: det er kommunen som tar kontakt.
public static class Tildelingsvarsel
{
    public static async Task<List<TildelingsvarselViewModel>> HentNye(ApplicationDbContext db, string brukerId)
    {
        var varsler = new List<TildelingsvarselViewModel>();
        if (string.IsNullOrEmpty(brukerId))
        {
            return varsler;
        }

        // Ressursene brukeren eier (r => ... betyr «for hver ressurs r»).
        List<Ressurs> ressurser = await db.Ressurser.AsNoTracking()
            .Where(r => r.OpprettetAvBrukerId == brukerId)
            .ToListAsync();

        if (ressurser.Count == 0)
        {
            return varsler;
        }

        var ressursIder = new List<int>();
        foreach (Ressurs ressurs in ressurser)
        {
            ressursIder.Add(ressurs.Id);
        }

        // Tildelinger av disse ressursene som eieren ikke har sett ennå, nyeste først.
        List<Tildeling> nye = await db.Tildelinger.AsNoTracking()
            .Where(t => ressursIder.Contains(t.RessursId) && !t.EierHarSett)
            .OrderByDescending(t => t.TildeltTid)
            .ToListAsync();

        foreach (Tildeling tildeling in nye)
        {
            Ressurs? ressurs = FinnRessurs(ressurser, tildeling.RessursId);
            Behov? behov = await db.Behovsliste.FindAsync(tildeling.BehovId);
            if (ressurs == null || behov == null)
            {
                continue;
            }

            var varsel = new TildelingsvarselViewModel
            {
                TildelingId = tildeling.Id,
                Ressurs = Visningstekst.RessursType(ressurs.Type, ressurs.AnnetType),
                Tid = tildeling.TildeltTid
            };
            if (Kommuner.Finnes(behov.Kommunenummer))
            {
                varsel.Kommune = Kommuner.VisNavn(behov.Kommunenummer);
            }
            varsler.Add(varsel);
        }

        return varsler;
    }

    // Antallet til merket på brukermenyen.
    public static async Task<int> AntallNye(ApplicationDbContext db, string brukerId)
    {
        List<TildelingsvarselViewModel> varsler = await HentNye(db, brukerId);
        return varsler.Count;
    }

    private static Ressurs? FinnRessurs(List<Ressurs> ressurser, int id)
    {
        foreach (Ressurs ressurs in ressurser)
        {
            if (ressurs.Id == id)
            {
                return ressurs;
            }
        }
        return null;
    }
}
