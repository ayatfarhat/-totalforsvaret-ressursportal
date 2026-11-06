using System.Security.Claims;

namespace Nabohjelp.Services;

// Én regel for hvem som er «ansatt» (Admin eller Kommune) og dermed kan se
// og endre andres behov og ressurser. Brukes både av policyene i Program.cs,
// i controllerne og i sidene, så regelen er lik overalt.
//
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

    private static bool HarTofaHvisDetKreves(ClaimsPrincipal bruker)
    {
        if (!KrevTofaForAnsatte)
        {
            return true;
        }
        return bruker.HasClaim("amr", "mfa");
    }
}
