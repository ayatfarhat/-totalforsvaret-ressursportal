using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;

namespace Nabohjelp.Services;

// GDPR sier at personopplysninger ikke skal lagres lenger enn nødvendig (lagringsbegrensning).
// Telefon og e-post på et behov trengs bare mens behovet blir fulgt opp.
// Når et behov har vært avsluttet lenge, fjerner vi derfor kontaktopplysningene.
// Selve behovet beholdes, slik at kommunen fortsatt har statistikk.
public static class PersonvernOpprydding
{
    // Kjøres når appen starter (se Program.cs). Returnerer hvor mange behov som ble ryddet.
    public static async Task<int> FjernGamleKontaktopplysninger(ApplicationDbContext db, int dager)
    {
        var naa = DateTime.UtcNow;

        // Henter bare behov som fortsatt har kontaktopplysninger.
        var behovMedKontakt = await db.Behovsliste
            .Where(b => b.KontaktTelefon != null || b.KontaktEpost != null)
            .ToListAsync();

        int antall = 0;
        foreach (var behov in behovMedKontakt)
        {
            if (SkalFjerneKontaktinfo(behov, naa, dager))
            {
                behov.KontaktTelefon = null;
                behov.KontaktEpost = null;
                antall++;
            }
        }

        if (antall > 0)
        {
            await db.SaveChangesAsync();
        }

        return antall;
    }

    // Et behov er "avsluttet" når det er løst, avvist eller trukket tilbake.
    // Kontaktinformasjonen fjernes når det er gått flere dager enn grensen siden siste endring.
    // Egen metode, slik at regelen kan testes uten database (se Personvern_Tests.cs).
    public static bool SkalFjerneKontaktinfo(Behov behov, DateTime naa, int dager)
    {
        bool avsluttet =
            behov.Status == BehovStatus.Resolved ||
            behov.Status == BehovStatus.Rejected ||
            behov.Status == BehovStatus.Withdrawn;

        if (!avsluttet)
        {
            return false;
        }

        DateTime sistEndret = behov.SistEndretTid ?? behov.OpprettetTid;
        return (naa - sistEndret).TotalDays > dager;
    }
}
