using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;

namespace Nabohjelp.Services;

// Samler det som har med personopplysningene til én bruker å gjøre:
// sletting av hele kontoen og nedlasting av "mine data".
// Brukes både når brukeren sletter sin egen konto og når admin sletter en bruker.
public class KontoService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _miljo;

    public KontoService(UserManager<IdentityUser> userManager, ApplicationDbContext db, IWebHostEnvironment miljo)
    {
        _userManager = userManager;
        _db = db;
        _miljo = miljo;
    }

    // Sletter brukeren og alle behov og ressurser brukeren har registrert.
    // Returnerer null hvis alt gikk bra, ellers en feilmelding.
    public async Task<string?> SlettBrukerOgData(IdentityUser bruker)
    {
        var behov = await _db.Behovsliste
            .Where(b => b.OpprettetAvBrukerId == bruker.Id)
            .ToListAsync();

        var ressurser = await _db.Ressurser
            .Where(r => r.OpprettetAvBrukerId == bruker.Id)
            .ToListAsync();

        var behovIder = behov.Select(b => b.Id).ToList();
        var ressursIder = ressurser.Select(r => r.Id).ToList();

        // Tildelinger er historikk for kriseledelsen og skal ikke forsvinne automatisk.
        bool harTildelinger = await _db.Tildelinger.AnyAsync(t =>
            t.TildeltAvBrukerId == bruker.Id ||
            behovIder.Contains(t.BehovId) ||
            ressursIder.Contains(t.RessursId));

        if (harTildelinger)
        {
            return "Kontoen har tilknyttede tildelinger og kan ikke slettes automatisk. Kontakt administrator.";
        }

        // Alt slettes i én transaksjon: enten blir alt slettet, eller ingenting.
        await using var transaksjon = await _db.Database.BeginTransactionAsync();

        try
        {
            _db.Behovsliste.RemoveRange(behov);
            _db.Ressurser.RemoveRange(ressurser);
            await _db.SaveChangesAsync();

            // Identity sletter kontoen og rollekoblingene.
            var resultat = await _userManager.DeleteAsync(bruker);
            if (!resultat.Succeeded)
            {
                await transaksjon.RollbackAsync();
                return "Kunne ikke slette kontoen. Prøv igjen.";
            }

            await transaksjon.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaksjon.RollbackAsync();
            return "Slettingen kunne ikke fullføres. Kontoen kan ha fått nye tilknyttede registreringer. Prøv igjen eller kontakt administrator.";
        }

        // Bildene til ressursene skal heller ikke bli liggende igjen på serveren.
        SlettRessursBilder(ressursIder);
        return null;
    }

    // Lager en JSON-fil med alt vi har lagret om brukeren (GDPR: rett til innsyn og dataportabilitet).
    public async Task<byte[]> LagMineData(IdentityUser bruker)
    {
        var roller = await _userManager.GetRolesAsync(bruker);

        var behov = await _db.Behovsliste
            .Where(b => b.OpprettetAvBrukerId == bruker.Id)
            .OrderBy(b => b.OpprettetTid)
            .ToListAsync();

        var ressurser = await _db.Ressurser
            .Where(r => r.OpprettetAvBrukerId == bruker.Id)
            .OrderBy(r => r.OpprettetTid)
            .ToListAsync();

        var data = new
        {
            LastetNed = DateTime.UtcNow,
            Konto = new
            {
                Epost = bruker.Email,
                Roller = roller,
                TofaErPa = bruker.TwoFactorEnabled
                // Passordet lagres bare som en hash og tas derfor ikke med.
            },
            Behov = behov.Select(b => new
            {
                b.Id,
                Type = b.Type.ToString(),
                b.AnnetType,
                b.Beskrivelse,
                b.Latitude,
                b.Longitude,
                Prioritet = b.Prioritet.ToString(),
                Status = b.Status.ToString(),
                b.KontaktTelefon,
                b.KontaktEpost,
                b.OpprettetTid,
                b.SistEndretTid
            }),
            Ressurser = ressurser.Select(r => new
            {
                r.Id,
                r.Type,
                r.Posisjon,
                r.Kommunenummer,
                r.Breddegrad,
                r.Lengdegrad,
                r.TilgjengeligFra,
                r.Beskrivelse,
                r.Navn,
                r.Telefon,
                r.Status,
                r.OpprettetTid
            })
        };

        var valg = new JsonSerializerOptions
        {
            WriteIndented = true,
            // Skriver æ, ø og å som vanlige bokstaver i filen.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.SerializeToUtf8Bytes(data, valg);
    }

    // Bildene ligger i wwwroot/uploads/ressurser og heter ressurs-ID + endelse (for eksempel 12.jpg).
    private void SlettRessursBilder(IEnumerable<int> ressursIder)
    {
        string mappe = Path.Combine(_miljo.WebRootPath, "uploads", "ressurser");
        foreach (int id in ressursIder)
        {
            foreach (string endelse in new[] { ".jpg", ".png", ".webp" })
            {
                string fil = Path.Combine(mappe, id + endelse);
                if (File.Exists(fil))
                {
                    File.Delete(fil);
                }
            }
        }
    }
}
