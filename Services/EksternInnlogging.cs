using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Nabohjelp.Services;

// Innlogging med Vipps og Apple («ekstern innlogging»).
//
// Begge bruker standarden OpenID Connect:
//   1. Brukeren trykker «Logg inn med Vipps» og sendes til Vipps.
//   2. Brukeren godkjenner i Vipps-appen.
//   3. Vipps sender brukeren tilbake til oss (/signin-vipps) med en engangskode.
//   4. ASP.NET bytter koden mot opplysninger om brukeren (e-post, navn) direkte hos Vipps.
//   5. AccountController.LoggInnMedSvar finner eller lager kontoen og logger brukeren inn.
// Vi får aldri passordet til brukeren, og brukeren trenger ikke lage et nytt passord hos oss.
//
// For å bruke dette må man ha avtale med leverandøren:
//   - Vipps: bedriftsavtale med Vipps MobilePay (org.nummer) og et salgssted med «Logg inn».
//   - Apple: medlemskap i Apple Developer Program og et ekte domene med https (ikke localhost).
// Nøklene (ClientId og ClientSecret) legges i appsettings under «EksternInnlogging».
// Mangler nøklene, er leverandøren av, og knappen viser bare en melding (se AccountController).
public static class EksternInnlogging
{
    public const string Vipps = "Vipps";
    public const string Apple = "Apple";

    // Leverandørene som har nøkler og er slått på. Fylles i LeggTil.
    private static readonly List<string> Aktive = new List<string>();

    // Sier om navnet er en av leverandørene vi kjenner.
    public static bool Finnes(string? leverandor)
    {
        return leverandor == Vipps || leverandor == Apple;
    }

    // Sier om leverandøren er slått på (har nøkler i appsettings).
    public static bool ErAktiv(string? leverandor)
    {
        return leverandor != null && Aktive.Contains(leverandor);
    }

    // Kalles fra Program.cs. Slår på Vipps og Apple hvis nøklene finnes.
    public static void LeggTil(WebApplicationBuilder builder)
    {
        AuthenticationBuilder innlogging = builder.Services.AddAuthentication();

        IConfigurationSection vipps = builder.Configuration.GetSection("EksternInnlogging:Vipps");
        if (!string.IsNullOrWhiteSpace(vipps["ClientId"]))
        {
            innlogging.AddOpenIdConnect(Vipps, "Vipps", options => SettOppVipps(options, vipps));
            Aktive.Add(Vipps);
        }

        IConfigurationSection apple = builder.Configuration.GetSection("EksternInnlogging:Apple");
        if (!string.IsNullOrWhiteSpace(apple["ClientId"]))
        {
            innlogging.AddOpenIdConnect(Apple, "Apple", options => SettOppApple(options, apple));
            Aktive.Add(Apple);
        }
    }

    // Vipps Login. Testmiljøet er apitest.vipps.no, det ekte er api.vipps.no.
    private static void SettOppVipps(OpenIdConnectOptions options, IConfigurationSection innstillinger)
    {
        // Adressen til Vipps. Står den ikke i appsettings, brukes testmiljøet.
        options.Authority = innstillinger["Authority"];
        if (string.IsNullOrEmpty(options.Authority))
        {
            options.Authority = "https://apitest.vipps.no/access-management-1.0/access/";
        }
        options.ClientId = innstillinger["ClientId"];
        options.ClientSecret = innstillinger["ClientSecret"];
        options.CallbackPath = "/signin-vipps";

        // «code» = vi får en engangskode i nettleseren og henter opplysningene direkte fra Vipps.
        options.ResponseType = OpenIdConnectResponseType.Code;

        // Vi ber bare om det vi trenger (dataminimering): e-post til kontoen.
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");

        // Vipps gir e-posten fra «userinfo», ikke i selve id-tokenet.
        options.GetClaimsFromUserInfoEndpoint = true;

        // Innloggingen lagres først i en midlertidig kake som Identity leser i LoggInnMedSvar.
        options.SignInScheme = IdentityConstants.ExternalScheme;
    }

    // Sign in with Apple.
    // ClientSecret er ikke et fast passord, men et signert token (JWT) som lages med
    // nøkkelfilen (.p8) fra Apple Developer og må fornyes minst hver sjette måned.
    private static void SettOppApple(OpenIdConnectOptions options, IConfigurationSection innstillinger)
    {
        options.Authority = "https://appleid.apple.com";
        options.ClientId = innstillinger["ClientId"];
        options.ClientSecret = innstillinger["ClientSecret"];
        options.CallbackPath = "/signin-apple";

        options.ResponseType = OpenIdConnectResponseType.Code;

        // Apple sender svaret som et skjema (POST) når vi ber om e-post.
        options.ResponseMode = OpenIdConnectResponseMode.FormPost;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");

        // Apple har ikke «userinfo». E-posten ligger i id-tokenet.
        options.GetClaimsFromUserInfoEndpoint = false;

        options.SignInScheme = IdentityConstants.ExternalScheme;
    }
}
