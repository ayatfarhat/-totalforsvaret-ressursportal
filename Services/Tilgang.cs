using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Nabohjelp.Services;

// Reglene for hvem som får se og endre hva. Brukes av policyene i Program.cs,
// i controllerne og i sidene, så reglene er like overalt.
//
// «Ansatt» betyr rollen Admin eller Kommune. De kan se og endre andres behov og ressurser.
// Når Sikkerhet:KrevTofaForAnsatte er true (appsettings.json), må den ansatte i tillegg
// ha logget inn med tofaktor. Identity merker en slik innlogging med "amr" = "mfa".
public static class Tilgang
{
    // Settes én gang i Program.cs ut fra appsettings.
    public static bool KrevTofaForAnsatte { get; set; }

    // Admin eller Kommune (med 2FA hvis det kreves).
    public static bool ErAnsatt(ClaimsPrincipal bruker)
    {
        bool harRolle = bruker.IsInRole("Admin") || bruker.IsInRole("Kommune");
        return harRolle && HarTofaHvisDetKreves(bruker);
    }

    // Bare Admin (med 2FA hvis det kreves).
    public static bool ErAdmin(ClaimsPrincipal bruker)
    {
        return bruker.IsInRole("Admin") && HarTofaHvisDetKreves(bruker);
    }

    // ID-en til brukeren som er logget inn, eller null hvis ingen er logget inn.
    public static string? BrukerId(ClaimsPrincipal bruker)
    {
        return bruker.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    // Sjekker om noen er logget inn.
    public static bool ErInnlogget(ClaimsPrincipal bruker)
    {
        return !string.IsNullOrEmpty(BrukerId(bruker));
    }

    // E-posten (brukernavnet) til den som er logget inn, eller tom tekst.
    public static string Epost(ClaimsPrincipal bruker)
    {
        string? navn = bruker.FindFirstValue(ClaimTypes.Name);
        if (navn == null)
        {
            return "";
        }
        return navn;
    }

    // Sjekker om brukeren laget registreringen (behovet eller ressursen).
    public static bool ErEier(ClaimsPrincipal bruker, string? eierId)
    {
        string? brukerId = BrukerId(bruker);
        if (string.IsNullOrEmpty(brukerId))
        {
            return false;
        }
        return brukerId == eierId;
    }

    // Den som laget registreringen, og kriseledelsen (Admin og Kommune) kan endre den.
    public static bool KanEndre(ClaimsPrincipal bruker, string? eierId)
    {
        return ErAnsatt(bruker) || ErEier(bruker, eierId);
    }

    // Innloggingen sjekkes mot databasen hvert minutt (se Program.cs), og lages da på nytt.
    // Denne metoden tar med merket for at brukeren logget inn med 2FA ("amr" = "mfa"),
    // så det ikke forsvinner ved sjekken.
    public static Task BeholdTofaMerke(SecurityStampRefreshingPrincipalContext context)
    {
        if (context.CurrentPrincipal == null || context.NewPrincipal == null)
        {
            return Task.CompletedTask;
        }

        Claim? merke = context.CurrentPrincipal.FindFirst("amr");

        // «as» gir null hvis Identity ikke er en ClaimsIdentity.
        ClaimsIdentity? nyInnlogging = context.NewPrincipal.Identity as ClaimsIdentity;

        if (merke != null && nyInnlogging != null && !nyInnlogging.HasClaim("amr", merke.Value))
        {
            nyInnlogging.AddClaim(new Claim("amr", merke.Value));
        }

        return Task.CompletedTask;
    }

    private static bool HarTofaHvisDetKreves(ClaimsPrincipal bruker)
    {
        if (!KrevTofaForAnsatte)
        {
            return true;
        }
        return bruker.HasClaim("amr", "mfa");
    }
}
