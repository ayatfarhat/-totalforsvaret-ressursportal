using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;

namespace Nabohjelp.Services;

// Krypterer personopplysninger før de lagres i databasen.
//
// Gjelder telefon og e-post på behov, og navn og telefon på ressurser.
// Hvis noen får tak i databasen (for eksempel en sikkerhetskopi), ser de bare
// uleselig tekst i disse feltene. Appen dekrypterer automatisk når den leser dem.
//
// Selve krypteringen gjøres av ASP.NET Core Data Protection (AES-256 med en
// kontrollsum, så ingen kan endre den krypterte teksten uten at det oppdages).
// Nøklene lagres utenfor databasen, i mappen «nokler» (se Program.cs og docker-compose.yml).
//
// Koblingen til databasen står i ApplicationDbContext: der sier vi at disse feltene
// skal gå gjennom Krypter når de lagres, og Dekrypter når de leses.
public static class Kryptering
{
    // Alle krypterte verdier starter med dette. Da kan vi skille dem fra
    // gamle verdier som ble lagret før krypteringen fantes.
    public const string Merke = "kryptert:";

    // Settes én gang i Program.cs. Er den ikke satt (for eksempel i enhetstestene
    // som ikke tester kryptering), lagres teksten som den er.
    public static IDataProtector? Beskytter { get; set; }

    // Gjør teksten om til kryptert tekst, for eksempel
    // "91234567" -> "kryptert:CfDJ8N...".
    public static string? Krypter(string? tekst)
    {
        return Krypter(tekst, Beskytter);
    }

    // Samme som over, men med en bestemt beskytter. Brukes av enhetstestene,
    // så de ikke må endre Beskytter, som hele appen deler.
    public static string? Krypter(string? tekst, IDataProtector? beskytter)
    {
        if (tekst == null || beskytter == null)
        {
            return tekst;
        }

        // Allerede kryptert, så vi krypterer ikke to ganger.
        if (tekst.StartsWith(Merke))
        {
            return tekst;
        }

        return Merke + beskytter.Protect(tekst);
    }

    // Gjør kryptert tekst om til vanlig tekst igjen.
    public static string? Dekrypter(string? lagret)
    {
        return Dekrypter(lagret, Beskytter);
    }

    public static string? Dekrypter(string? lagret, IDataProtector? beskytter)
    {
        if (lagret == null || beskytter == null)
        {
            return lagret;
        }

        // Gammel verdi fra før krypteringen fantes. Den krypteres neste gang
        // appen starter (se KrypterGamleOpplysninger under).
        if (!lagret.StartsWith(Merke))
        {
            return lagret;
        }

        try
        {
            return beskytter.Unprotect(lagret.Substring(Merke.Length));
        }
        catch (CryptographicException)
        {
            // Nøkkelen finnes ikke lenger (for eksempel hvis mappen med nøkler er slettet),
            // eller noen har endret teksten i databasen. Da viser vi en melding
            // i stedet for at hele siden krasjer.
            return Oversetter.Tekst("(kan ikke vises)");
        }
    }

    // Sier om en verdi er lagret kryptert. Brukes i testene.
    public static bool ErKryptert(string? lagret)
    {
        return lagret != null && lagret.StartsWith(Merke);
    }

    // Krypterer opplysninger som ble lagret før krypteringen kom på plass.
    // Kalles når appen starter (se Program.cs).
    //
    // SQL-spørringene finner radene der et felt har en verdi som ikke starter med Merke,
    // altså ukryptert tekst. Ved å si at feltene er «endret», lagrer EF dem på nytt,
    // og da går de gjennom Krypter. Rader som allerede er kryptert, røres ikke.
    public static async Task KrypterGamleOpplysninger(ApplicationDbContext db)
    {
        if (Beskytter == null)
        {
            return;
        }

        List<Behov> behovliste = await db.Behovsliste
            .FromSqlRaw(
                "SELECT * FROM Behovsliste " +
                "WHERE KontaktTelefon NOT LIKE 'kryptert:%' OR KontaktEpost NOT LIKE 'kryptert:%'")
            .ToListAsync();

        foreach (Behov behov in behovliste)
        {
            db.Entry(behov).Property(b => b.KontaktTelefon).IsModified = true;
            db.Entry(behov).Property(b => b.KontaktEpost).IsModified = true;
        }

        List<Ressurs> ressurser = await db.Ressurser
            .FromSqlRaw(
                "SELECT * FROM Ressurser " +
                "WHERE Navn NOT LIKE 'kryptert:%' OR Telefon NOT LIKE 'kryptert:%'")
            .ToListAsync();

        foreach (Ressurs ressurs in ressurser)
        {
            db.Entry(ressurs).Property(r => r.Navn).IsModified = true;
            db.Entry(ressurs).Property(r => r.Telefon).IsModified = true;
        }

        await db.SaveChangesAsync();
    }
}
