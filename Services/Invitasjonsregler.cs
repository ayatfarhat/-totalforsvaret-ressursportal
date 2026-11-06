using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;
using Nabohjelp.Models;

namespace Nabohjelp.Services;

// Reglene for invitasjonslenker (se Models/Invitasjon.cs).
// Brukes av AdminController (lage lenker) og AccountController (ta imot en lenke).
public static class Invitasjonsregler
{
    // Rollene en invitasjon kan gi. Frivillig får alle uansett når de registrerer seg.
    public static readonly string[] Roller = { "Kommune", "Admin" };

    // Hvor mange dager en lenke kan gjelde. Admin velger ett av disse.
    public static readonly int[] GyldigDager = { 1, 3, 7 };

    // Lager en tilfeldig kode til lenken: 32 tilfeldige byte, skrevet som 43 tegn.
    // Den er umulig å gjette, og den inneholder bare tegn som kan stå i en nettadresse.
    public static string LagKode()
    {
        byte[] tilfeldig = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tilfeldig)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    // Fingeravtrykket (SHA-256) av koden, som er det vi lagrer i databasen.
    // Samme kode gir alltid samme fingeravtrykk, men man kan ikke regne seg tilbake til koden.
    public static string LagHash(string kode)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(kode));
        return Convert.ToHexString(hash);
    }

    // Finner invitasjonen som hører til koden i lenken, eller null.
    public static async Task<Invitasjon?> FinnAsync(ApplicationDbContext db, string? kode)
    {
        if (string.IsNullOrWhiteSpace(kode))
        {
            return null;
        }

        string hash = LagHash(kode);
        return await db.Invitasjoner.FirstOrDefaultAsync(i => i.KodeHash == hash);
    }

    // Sjekker om lenken kan brukes nå. Gir en feilmelding, eller null hvis den er gyldig.
    public static string? SjekkGyldig(Invitasjon? invitasjon, DateTime naa)
    {
        if (invitasjon == null)
        {
            return "Invitasjonen finnes ikke. Sjekk at du har kopiert hele lenken.";
        }

        if (invitasjon.BruktTid != null)
        {
            return "Invitasjonen er allerede brukt. Be administrator om en ny lenke.";
        }

        if (invitasjon.UtloperTid <= naa)
        {
            return "Invitasjonen har gått ut. Be administrator om en ny lenke.";
        }

        return null;
    }

    // Sjekker om brukeren kan ta imot rollen. Gir en feilmelding, eller null hvis det er greit.
    // Admin kan ikke bruke en lenke, så en administrator ikke mister Admin-rollen ved et uhell.
    public static string? SjekkBruker(IList<string> rollerNaa, string nyRolle)
    {
        if (rollerNaa.Contains("Admin"))
        {
            return "Du er administrator og kan ikke bruke en invitasjon.";
        }

        if (rollerNaa.Contains(nyRolle))
        {
            return "Du har allerede denne rollen.";
        }

        return null;
    }
}
