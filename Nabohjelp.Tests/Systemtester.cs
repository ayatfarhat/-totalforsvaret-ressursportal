using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace Nabohjelp.Tests;

// Systemtester: tester hele appen slik en bruker møter den, gjennom nettleserens
// forespørsler (GET og POST), med ekte database (MariaDB) og innlogging.
//
// Testene trenger at appen kjører, for eksempel med «docker compose up».
// Adressen settes i miljøvariabelen SYSTEMTEST_URL (for eksempel http://localhost:8080).
// Er den ikke satt, hopper testene over seg selv, så «dotnet test» virker uten Docker.
// I GitHub Actions starter jobben «systemtest» appen med Docker og kjører dem (se .github/workflows/ci.yml).
//
// Testene bruker testbrukerne fra Data/SeedData.cs, som bare finnes i Development.
[Trait("Kategori", "Systemtest")]
public class Systemtester : IClassFixture<SystemtestOppsett>
{
    private readonly SystemtestOppsett _app;

    public Systemtester(SystemtestOppsett app)
    {
        _app = app;
    }

    [Fact]
    public async Task Forsiden_HarSikkerhetshoder()
    {
        if (!_app.ErAktiv) return;

        var svar = await _app.Anonym.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Equal("DENY", Hode(svar, "X-Frame-Options"));
        Assert.Equal("nosniff", Hode(svar, "X-Content-Type-Options"));
        Assert.Contains("frame-ancestors 'none'", Hode(svar, "Content-Security-Policy"));
        // Appen skal ikke fortelle hvilken webserver den bruker.
        Assert.False(svar.Headers.Contains("Server"));
    }

    [Fact]
    public async Task IkkeInnlogget_SendesTilInnlogging()
    {
        if (!_app.ErAktiv) return;

        foreach (string side in new[] { "/Behov/Opprett", "/Ressurs", "/Kommandobro", "/Admin" })
        {
            var svar = await _app.Anonym.GetAsync(side);

            Assert.Equal(HttpStatusCode.Redirect, svar.StatusCode);
            Assert.Contains("/Account/Login", svar.Headers.Location?.ToString());
        }
    }

    [Fact]
    public async Task Frivillig_FarIkkeTilgangTilKriseledelsensSider()
    {
        if (!_app.ErAktiv) return;

        foreach (string side in new[] { "/Kommandobro", "/Behov/Ko", "/Tildeling", "/Ressurs/Utskrift", "/Admin" })
        {
            var svar = await _app.Frivillig.GetAsync(side);

            Assert.Equal(HttpStatusCode.Redirect, svar.StatusCode);
            Assert.Contains("/Account/AccessDenied", svar.Headers.Location?.ToString());
        }
    }

    [Fact]
    public async Task Kommune_FarIkkeTilgangTilBrukeradministrasjon()
    {
        if (!_app.ErAktiv) return;

        var kommandobro = await _app.Kommune.GetAsync("/Kommandobro");
        var admin = await _app.Kommune.GetAsync("/Admin");

        Assert.Equal(HttpStatusCode.OK, kommandobro.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
        Assert.Contains("/Account/AccessDenied", admin.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Skjema_UtenAntiForgeryToken_Avvises()
    {
        if (!_app.ErAktiv) return;

        // Et skjema sendt fra en annen nettside har ikke den skjulte koden (token) fra vår side.
        var svar = await _app.Frivillig.PostAsync("/Home/VelgSprak",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["sprak"] = "en" }));

        Assert.Equal(HttpStatusCode.BadRequest, svar.StatusCode);
    }

    [Fact]
    public async Task Skjema_MedSkjultFeltUtfylt_AvvisesSomSoppelpost()
    {
        if (!_app.ErAktiv) return;

        // Mennesker ser ikke feltet «Nettside». Roboter fyller det ut (se Services/Sikkerhet.cs).
        var svar = await SystemtestOppsett.SendSkjema(_app.Frivillig, "/Behov/Opprett", new Dictionary<string, string>
        {
            ["Type"] = "Generator",
            ["Prioritet"] = "Urgent",
            ["Latitude"] = "58.34",
            ["Longitude"] = "8.59",
            ["KontaktTelefon"] = "41234567",
            ["Nettside"] = "https://reklame.example"
        });

        Assert.Equal(HttpStatusCode.BadRequest, svar.StatusCode);
    }

    [Fact]
    public async Task FeilPassord_GirFeilmeldingOgIngenInnlogging()
    {
        if (!_app.ErAktiv) return;

        var klient = _app.NyKlient();
        var svar = await SystemtestOppsett.SendSkjema(klient, "/Account/Login", new Dictionary<string, string>
        {
            ["Epost"] = "finnes.ikke@test.no",
            ["Passord"] = "FeilPassord1!"
        });

        // Siden vises på nytt (200) med feilmelding, i stedet for å sende brukeren videre.
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Contains("Feil e-post eller passord", await svar.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Hovedflyt_MeldeBehov_RegistrereRessurs_Tildele()
    {
        if (!_app.ErAktiv) return;

        // En unik tekst, så vi finner igjen akkurat våre registreringer i databasen.
        string merke = "Systemtest " + Guid.NewGuid().ToString("N").Substring(0, 8);

        // 1. Frivillig melder et akutt behov for strøm i Grimstad.
        var behovSvar = await SystemtestOppsett.SendSkjema(_app.Frivillig, "/Behov/Opprett", new Dictionary<string, string>
        {
            ["Type"] = "Generator",
            ["Prioritet"] = "Urgent",
            ["Latitude"] = "58.3405",
            ["Longitude"] = "8.5934",
            ["Beskrivelse"] = merke,
            ["Landskode"] = "+47",
            ["KontaktTelefon"] = "41234567"
        });
        Assert.Equal(HttpStatusCode.Redirect, behovSvar.StatusCode);

        // 2. Frivillig registrerer et aggregat i nærheten.
        var ressursSvar = await SystemtestOppsett.SendSkjema(_app.Frivillig, "/Ressurs/Create", new Dictionary<string, string>
        {
            ["Type"] = "Aggregat",
            ["KommuneNavn"] = "Grimstad",
            ["Posisjon"] = merke,
            ["Breddegrad"] = "58.35",
            ["Lengdegrad"] = "8.60",
            ["TilgjengeligFra"] = DateTime.Now.AddDays(1).ToString("yyyy-MM-ddTHH:mm"),
            ["Beskrivelse"] = "Effekt 6 kW",
            ["Navn"] = "Ola Nordmann",
            ["Landskode"] = "+47",
            ["Telefon"] = "41234567"
        });
        Assert.Equal(HttpStatusCode.Redirect, ressursSvar.StatusCode);
        string ressursId = SisteTall(ressursSvar.Headers.Location!.ToString());

        // 3. Kommunen finner behovet i tildelingslista.
        string tildelingsliste = await SystemtestOppsett.Hent(_app.Kommune, "/Tildeling");
        string behovId = FinnId(tildelingsliste, merke, @"/Tildeling/Match/(\d+)");

        // 4. Matchsiden viser aggregatet, og kommunen tildeler det.
        string matchside = await SystemtestOppsett.Hent(_app.Kommune, "/Tildeling/Match/" + behovId);
        Assert.Contains(merke, matchside);

        var tildelSvar = await SystemtestOppsett.SendSkjema(_app.Kommune, "/Tildeling/Tildel", new Dictionary<string, string>
        {
            ["behovId"] = behovId,
            ["ressursId"] = ressursId
        }, skjemaFra: "/Tildeling/Match/" + behovId);
        Assert.Equal(HttpStatusCode.Redirect, tildelSvar.StatusCode);

        // 5. Behovet har nå status «Tildelt», og ressursen er ikke lenger ledig.
        string etterTildeling = await SystemtestOppsett.Hent(_app.Kommune, "/Tildeling");
        Assert.Contains("Tildelt", Rad(etterTildeling, merke));

        string ressursliste = await SystemtestOppsett.Hent(_app.Frivillig, "/Ressurs");
        Assert.Contains("Tildelt", Rad(ressursliste, merke));

        // 6. Den som eier ressursen, får et varsel på forsiden.
        string forside = await SystemtestOppsett.Hent(_app.Frivillig, "/");
        Assert.Contains("Ressursen din er tildelt", forside);

        // 7. Kommunen angrer tildelingen fra behovskøen (fanen «Pågår»).
        string ko = await SystemtestOppsett.Hent(_app.Kommune, "/Behov/Ko?kategori=pagar");
        string tildelingId = FinnId(ko, merke, @"action=""/Tildeling/Angre""[^>]*>\s*<input type=""hidden"" name=""id"" value=""(\d+)""");

        var angreSvar = await SystemtestOppsett.SendSkjema(_app.Kommune, "/Tildeling/Angre", new Dictionary<string, string>
        {
            ["id"] = tildelingId
        }, skjemaFra: "/Behov/Ko?kategori=pagar");
        Assert.Equal(HttpStatusCode.Redirect, angreSvar.StatusCode);

        // 8. Ressursen er ledig igjen, og historikken viser at tildelingen ble angret.
        string etterAngring = await SystemtestOppsett.Hent(_app.Frivillig, "/Ressurs");
        Assert.Contains("Tilgjengelig", Rad(etterAngring, merke));

        string mineBehov = await SystemtestOppsett.Hent(_app.Frivillig, "/Behov/MineBehov");
        Assert.Contains("Tildelingen av Aggregat ble angret", mineBehov);
    }

    [Fact]
    public async Task Invitasjonslenke_GirRollenEnGang()
    {
        if (!_app.ErAktiv) return;

        // 1. Admin lager en lenke som gir rollen Kommune. Den vises bare én gang, rett etterpå.
        var lagSvar = await SystemtestOppsett.SendSkjema(_app.Admin, "/Admin/LagInvitasjon", new Dictionary<string, string>
        {
            ["rolle"] = "Kommune",
            ["dager"] = "1"
        }, skjemaFra: "/Admin/Invitasjoner");
        Assert.Equal(HttpStatusCode.Redirect, lagSvar.StatusCode);

        string adminside = await SystemtestOppsett.Hent(_app.Admin, "/Admin/Invitasjoner");
        var lenke = Regex.Match(adminside, @"/Account/Invitasjon\?kode=([A-Za-z0-9_\-]+)");
        Assert.True(lenke.Success);
        string adresse = "/Account/Invitasjon?kode=" + lenke.Groups[1].Value;

        // 2. En ny bruker registrerer seg (blir Frivillig) og har ikke tilgang til Kommandobro.
        var ny = _app.NyKlient();
        string epost = "ny" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@test.no";
        var registrer = await SystemtestOppsett.SendSkjema(ny, "/Account/Register", new Dictionary<string, string>
        {
            ["Epost"] = epost,
            ["Passord"] = "Systemtest1!",
            ["BekreftPassord"] = "Systemtest1!",
            ["GodtarPersonvern"] = "true"
        });
        Assert.Equal(HttpStatusCode.Redirect, registrer.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await ny.GetAsync("/Kommandobro")).StatusCode);

        // 3. Brukeren åpner lenken og tar imot rollen. Nå slipper hen inn på Kommandobro.
        var taImot = await SystemtestOppsett.SendSkjema(ny, "/Account/TaImotInvitasjon",
            new Dictionary<string, string> { ["kode"] = lenke.Groups[1].Value }, skjemaFra: adresse);
        Assert.Equal(HttpStatusCode.Redirect, taImot.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ny.GetAsync("/Kommandobro")).StatusCode);

        // 4. Lenken kan ikke brukes igjen.
        string igjen = await SystemtestOppsett.Hent(_app.Anonym, adresse);
        Assert.Contains("allerede brukt", igjen);
    }

    [Fact]
    public async Task Sprakvalg_ViserSidenPaEngelsk()
    {
        if (!_app.ErAktiv) return;

        var klient = _app.NyKlient();
        var svar = await SystemtestOppsett.SendSkjema(klient, "/Home/VelgSprak",
            new Dictionary<string, string> { ["sprak"] = "en" }, skjemaFra: "/");
        Assert.Equal(HttpStatusCode.Redirect, svar.StatusCode);

        string forside = await SystemtestOppsett.Hent(klient, "/");
        Assert.Contains("<title>Home", forside);
        Assert.Contains("lang=\"en\"", forside);
    }

    private static string Hode(HttpResponseMessage svar, string navn)
    {
        return svar.Headers.TryGetValues(navn, out var verdier) ? string.Join(",", verdier) : "";
    }

    // Tallet til slutt i en adresse, for eksempel 12 i /Ressurs/Confirmation/12.
    private static string SisteTall(string adresse)
    {
        return Regex.Match(adresse, @"(\d+)\s*$").Groups[1].Value;
    }

    // Finner tabellraden (<tr>) eller kortet som inneholder teksten.
    private static string Rad(string html, string tekst)
    {
        foreach (Match rad in Regex.Matches(html, @"<tr[\s>].*?</tr>", RegexOptions.Singleline))
        {
            if (rad.Value.Contains(tekst))
            {
                return rad.Value;
            }
        }
        return "";
    }

    // Finner ID-en i lenken på samme rad som teksten.
    private static string FinnId(string html, string tekst, string lenkemonster)
    {
        string rad = Rad(html, tekst);
        var treff = Regex.Match(rad, lenkemonster);
        Assert.True(treff.Success);
        return treff.Groups[1].Value;
    }
}

// Fartsgrensen testes for seg selv og til slutt (eget steg i GitHub Actions),
// fordi den bruker opp forsøkene for innlogging i ett minutt.
[Trait("Kategori", "Fartsgrense")]
public class FartsgrenseSystemtest
{
    [Fact]
    public async Task MangeInnloggingsforsok_GirFeilkode429()
    {
        string? adresse = Environment.GetEnvironmentVariable("SYSTEMTEST_URL");
        if (string.IsNullOrEmpty(adresse)) return;

        using var klient = new HttpClient { BaseAddress = new Uri(adresse) };
        var koder = new List<HttpStatusCode>();

        // Grensen er 10 forsøk per minutt (Services/Sikkerhet.cs). Forsøk nr. 11 skal stoppes.
        for (int i = 0; i < 11; i++)
        {
            var svar = await klient.PostAsync("/Account/Login",
                new FormUrlEncodedContent(new Dictionary<string, string> { ["Epost"] = "x@test.no", ["Passord"] = "x" }));
            koder.Add(svar.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, koder);
    }
}

// Felles oppsett for systemtestene: én innlogget klient per rolle.
// Innloggingen skjer bare én gang, fordi appen tillater 10 innloggingsforsøk i minuttet.
public class SystemtestOppsett : IAsyncLifetime
{
    public bool ErAktiv { get; private set; }
    public HttpClient Anonym { get; private set; } = null!;
    public HttpClient Frivillig { get; private set; } = null!;
    public HttpClient Kommune { get; private set; } = null!;
    public HttpClient Admin { get; private set; } = null!;

    private string _adresse = "";

    public async Task InitializeAsync()
    {
        _adresse = Environment.GetEnvironmentVariable("SYSTEMTEST_URL") ?? "";
        ErAktiv = _adresse != "";
        if (!ErAktiv) return;

        Anonym = NyKlient();
        Frivillig = await LoggInn("privatperson@test.no", "Privat123!");
        Kommune = await LoggInn("kommune@test.no", "Kommune123!");
        Admin = await LoggInn("admin@test.no", "Admin123!");
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    // En «nettleser» med egne cookies. Den følger ikke videresendinger av seg selv,
    // så testene kan sjekke hvor brukeren blir sendt.
    public HttpClient NyKlient()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = false
        };
        return new HttpClient(handler) { BaseAddress = new Uri(_adresse) };
    }

    private async Task<HttpClient> LoggInn(string epost, string passord)
    {
        var klient = NyKlient();
        var svar = await SendSkjema(klient, "/Account/Login", new Dictionary<string, string>
        {
            ["Epost"] = epost,
            ["Passord"] = passord
        });

        // Riktig passord sender brukeren videre (302). Feil gir siden på nytt (200).
        if (svar.StatusCode != HttpStatusCode.Redirect)
        {
            throw new Exception("Kunne ikke logge inn som " + epost + " (svar " + (int)svar.StatusCode + ").");
        }
        return klient;
    }

    // Henter en side som tekst. Razor skriver æ, ø og å som koder (for eksempel &#xF8;),
    // så vi gjør dem om til vanlige bokstaver før testene leter i teksten.
    public static async Task<string> Hent(HttpClient klient, string adresse)
    {
        string html = await klient.GetStringAsync(adresse);
        return WebUtility.HtmlDecode(html);
    }

    // Sender et skjema slik nettleseren gjør: henter først siden med skjemaet for å få
    // den skjulte koden (__RequestVerificationToken), og sender den med.
    public static async Task<HttpResponseMessage> SendSkjema(HttpClient klient, string adresse,
        Dictionary<string, string> felter, string? skjemaFra = null)
    {
        string side = await SystemtestOppsett.Hent(klient, skjemaFra ?? adresse);
        var token = Regex.Match(side, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!token.Success)
        {
            throw new Exception("Fant ikke skjemaet på " + (skjemaFra ?? adresse) + ".");
        }

        var data = new Dictionary<string, string>(felter)
        {
            ["__RequestVerificationToken"] = token.Groups[1].Value
        };
        return await klient.PostAsync(adresse, new FormUrlEncodedContent(data));
    }
}
