using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Nabohjelp.Services;

// Sikkerhetstiltak som gjelder hele appen. Kalles fra Program.cs.
// Tiltakene i controllerne (innlogging, tilgang, CSRF-token osv.) står i README under «Sikkerhet».
public static class Sikkerhet
{
    // Navnet på fartsgrensen. Brukes med [EnableRateLimiting(Sikkerhet.Innlogging)] i controllerne.
    public const string Innlogging = "Innlogging";

    // Hvor mange forsøk samme IP-adresse får per minutt.
    private const int MaksForsokPerMinutt = 10;

    // Begrenser hvor mange ganger samme IP-adresse kan prøve å logge inn, registrere seg
    // eller bruke en invitasjon. Det gjør det mye tregere å gjette passord eller koder.
    // For mange forsøk gir feilkode 429, som vises på Home/Status.
    public static void LeggTilFartsgrense(IServiceCollection tjenester)
    {
        // AddRateLimiter tar imot metoden som setter opp reglene (SettOppFartsgrense under).
        tjenester.AddRateLimiter(SettOppFartsgrense);
    }

    private static void SettOppFartsgrense(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy<string>(Innlogging, LagTellerForIpAdresse);
    }

    // Hver IP-adresse får sin egen teller, så én som prøver mye ikke stopper andre.
    private static RateLimitPartition<string> LagTellerForIpAdresse(HttpContext context)
    {
        string ipAdresse = "ukjent";
        if (context.Connection.RemoteIpAddress != null)
        {
            ipAdresse = context.Connection.RemoteIpAddress.ToString();
        }

        return RateLimitPartition.GetFixedWindowLimiter(ipAdresse, LagRegler);
    }

    // Reglene for hver teller: maks 10 forsøk, og telleren starter på null hvert minutt.
    private static FixedWindowRateLimiterOptions LagRegler(string ipAdresse)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = MaksForsokPerMinutt,
            Window = TimeSpan.FromMinutes(1)
        };
    }

    // Innholdet i Content-Security-Policy: hvor nettleseren får hente skript, stiler og bilder fra.
    // Bare fra oss selv og de få stedene vi bruker: Leaflet fra unpkg, QR-koden fra cdnjs
    // og kartbildene fra OpenStreetMap. 'unsafe-inline' trengs fordi noen sider har små
    // skript og stiler direkte i HTML-en.
    private const string InnholdsRegler =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://unpkg.com https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://unpkg.com; " +
        "img-src 'self' data: https://tile.openstreetmap.org https://unpkg.com; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    // Legger sikkerhetshoder på alle svar. De forteller nettleseren at:
    //   - siden ikke kan vises inne i en annen nettside (beskytter mot «clickjacking»),
    //   - den ikke skal gjette filtypen på filer (for eksempel opplastede bilder),
    //   - andre nettsider ikke får se hele adressen brukeren kom fra,
    //   - siden bare kan bruke posisjon (GPS), ikke kamera eller mikrofon,
    //   - skript og bilder bare kan hentes fra stedene i InnholdsRegler.
    public static void LeggTilSikkerhetshoder(WebApplication app)
    {
        // LeggPaHoder kjøres for hver forespørsel, før siden lages.
        app.Use(LeggPaHoder);
    }

    // next er resten av appen (controlleren som lager siden). Vi legger på hodene
    // og sender forespørselen videre.
    private static async Task LeggPaHoder(HttpContext context, Func<Task> next)
    {
        IHeaderDictionary hoder = context.Response.Headers;
        hoder["X-Frame-Options"] = "DENY";
        hoder["X-Content-Type-Options"] = "nosniff";
        hoder["Referrer-Policy"] = "strict-origin-when-cross-origin";
        hoder["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";
        hoder["Content-Security-Policy"] = InnholdsRegler;
        await next();
    }
}
