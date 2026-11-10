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
        DateTime naa = DateTime.UtcNow;

        // Henter bare behov som fortsatt har kontaktopplysninger.
        // b => ... betyr «for hvert behov b» (spørringen kjøres i databasen).
        List<Behov> behovMedKontakt = await db.Behovsliste
            .Where(b => b.KontaktTelefon != null || b.KontaktEpost != null)
            .ToListAsync();

        int antall = 0;
        foreach (Behov behov in behovMedKontakt)
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

    // Invitasjonslenker trengs ikke etter at de er brukt eller har gått ut. En brukt lenke
    // inneholder e-posten til den som brukte den, så vi sletter lenker som har vært
    // brukt eller utløpt lenger enn grensen. Returnerer hvor mange som ble slettet.
    public static async Task<int> SlettGamleInvitasjoner(ApplicationDbContext db, int dager)
    {
        DateTime naa = DateTime.UtcNow;
        List<Invitasjon> alle = await db.Invitasjoner.ToListAsync();

        int antall = 0;
        foreach (Invitasjon invitasjon in alle)
        {
            if (SkalSletteInvitasjon(invitasjon, naa, dager))
            {
                db.Invitasjoner.Remove(invitasjon);
                antall++;
            }
        }

        if (antall > 0)
        {
            await db.SaveChangesAsync();
        }

        return antall;
    }

    // En lenke er ferdig når den er brukt eller har gått ut. Den slettes når det er gått
    // flere dager enn grensen siden det skjedde.
    public static bool SkalSletteInvitasjon(Invitasjon invitasjon, DateTime naa, int dager)
    {
        DateTime ferdig = invitasjon.UtloperTid;
        if (invitasjon.BruktTid != null)
        {
            ferdig = invitasjon.BruktTid.Value;
        }

        return ferdig < naa && (naa - ferdig).TotalDays > dager;
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

        // Tiden for siste endring, eller når behovet ble meldt hvis det aldri er endret.
        DateTime sistEndret = behov.OpprettetTid;
        if (behov.SistEndretTid != null)
        {
            sistEndret = behov.SistEndretTid.Value;
        }

        return (naa - sistEndret).TotalDays > dager;
    }
}
