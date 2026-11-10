using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Nabohjelp;
using Nabohjelp.Data;
using Nabohjelp.Services;

// Tall (som koordinatene fra kartet) skal alltid leses med punktum som
// desimaltegn, uansett hvilket språk maskinen som kjører appen bruker.
// Uten denne kunne f.eks. "64.5" blitt feiltolket på en norsk maskin.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Om skrivemåten under: options => ... er en kort metode uten navn (en «lambda»).
// ASP.NET kaller den med et innstillingsobjekt (options), og vi setter verdiene vi vil ha.

// Ikke fortell hvilken webserver vi bruker (svarhodet «Server: Kestrel»).
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Språkvalg: tekstene på engelsk ligger i mappen Resources.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Legger til MVC
builder.Services.AddControllersWithViews(options =>
{
    // Feilmeldinger når en verdi ikke kan leses (for eksempel bokstaver i et tallfelt),
    // på norsk eller engelsk (se Services/Oversetter.cs).
    DefaultModelBindingMessageProvider meldinger = options.ModelBindingMessageProvider;
    meldinger.SetAttemptedValueIsInvalidAccessor((verdi, felt) => Oversetter.Tekst("«{0}» er ikke en gyldig verdi for {1}.", verdi, felt));
    meldinger.SetValueIsInvalidAccessor(verdi => Oversetter.Tekst("«{0}» er ikke en gyldig verdi.", verdi));
    meldinger.SetValueMustBeANumberAccessor(felt => Oversetter.Tekst("{0} må være et tall.", felt));
    meldinger.SetValueMustNotBeNullAccessor(verdi => Oversetter.Tekst("Feltet må fylles ut."));
    meldinger.SetMissingKeyOrValueAccessor(() => Oversetter.Tekst("Feltet må fylles ut."));
    meldinger.SetUnknownValueIsInvalidAccessor(felt => Oversetter.Tekst("Verdien er ikke gyldig for {0}.", felt));
    meldinger.SetNonPropertyAttemptedValueIsInvalidAccessor(verdi => Oversetter.Tekst("«{0}» er ikke en gyldig verdi.", verdi));
    meldinger.SetNonPropertyUnknownValueIsInvalidAccessor(() => Oversetter.Tekst("Verdien er ikke gyldig."));
    meldinger.SetNonPropertyValueMustBeANumberAccessor(() => Oversetter.Tekst("Feltet må være et tall."));
})
    // Gjør at sidene kan bruke @T["..."] for å vise tekst på valgt språk.
    .AddViewLocalization()
    // Oversetter også feilmeldingene i ViewModels (for eksempel «Skriv inn e-post»).
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    });

// Henter connection string fra appsettings.json eller Docker
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Kobler ApplicationDbContext til MariaDB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

// Identity + roller
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Låser kontoen i 5 minutter etter 5 feil passord på rad.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;

        // Én konto per e-postadresse.
        options.User.RequireUniqueEmail = true;

        // Samme passordregler som i skjemaene (RegisterViewModel og EndrePassordViewModel):
        // minst 8 tegn, med stor og liten bokstav, tall og spesialtegn.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Når admin endrer rolle, låser en konto eller sletter en bruker, skal det gjelde
// nesten med en gang. Standard er at innloggingen sjekkes på nytt hvert 30. minutt.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(1);

    // Ved sjekken lages innloggingen på nytt. Da tar vi med merket for at
    // brukeren logget inn med 2FA ("amr" = "mfa"), så det ikke forsvinner.
    options.OnRefreshingPrincipal = Tilgang.BeholdTofaMerke;
});

// Innloggings-cookien
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;               // JavaScript kan ikke lese cookien
    options.Cookie.SameSite = SameSiteMode.Lax;   // Beskytter mot falske skjemaer fra andre nettsider
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Admin og Kommune har tilgang til Kommandobroen og personopplysninger om andre.
// Når "KrevTofaForAnsatte" er true i appsettings, må de i tillegg ha logget inn med 2FA.
// Selve regelen står i Services/Tilgang.cs, så controllere og sider bruker den samme.
Tilgang.KrevTofaForAnsatte = builder.Configuration.GetValue<bool>("Sikkerhet:KrevTofaForAnsatte");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Ansatt", policy => policy.RequireAssertion(context => Tilgang.ErAnsatt(context.User)));
    options.AddPolicy("Admin", policy => policy.RequireAssertion(context => Tilgang.ErAdmin(context.User)));
});

// Sletting og nedlasting av brukerdata (brukes av AccountController og AdminController).
builder.Services.AddScoped<KontoService>();

// Maks 10 forsøk per minutt på innlogging, registrering og invitasjoner, og maks
// 20 nye behov eller ressurser per minutt for hver bruker (se Services/Sikkerhet.cs).
Sikkerhet.LeggTilFartsgrense(builder.Services);

WebApplication app = builder.Build();

// Gjør at modeller og tjenester også kan vise tekst på valgt språk (se Services/Oversetter.cs).
Oversetter.Localizer = app.Services.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<SharedResource>>();

// Kjører migrasjoner automatisk
// og oppretter rollene Admin, Kommune og Frivillig
using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDbContext dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();

    RoleManager<IdentityRole> roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    await RoleInitializer.InitializeAsync(roleManager);
}

// Lager testbrukere (med kjente passord) etter at databasen og rollene finnes.
// Bare under utvikling: Docker og «dotnet run» kjører som Development (se docker-compose.yml).
if (app.Environment.IsDevelopment())
{
    await SeedData.LagTestbrukere(app);
}

// GDPR: fjerner kontaktopplysninger fra gamle, avsluttede behov, og sletter
// gamle invitasjonslenker (de inneholder e-posten til den som brukte dem).
using (IServiceScope scope = app.Services.CreateScope())
{
    ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    int dager = app.Configuration.GetValue("Personvern:SlettKontaktinfoEtterDager", 90);
    await PersonvernOpprydding.FjernGamleKontaktopplysninger(db, dager);
    await PersonvernOpprydding.SlettGamleInvitasjoner(db, dager);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Sikkerhetshoder på alle svar, blant annet mot clickjacking og skript fra fremmede
// nettsider (se Services/Sikkerhet.cs).
Sikkerhet.LeggTilSikkerhetshoder(app);

// Viser en norsk side ved 404 og andre feilkoder i stedet for en tom side.
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

app.UseHttpsRedirection();

// Språket velges med knappen NO/EN og huskes i en cookie.
// Bare språket på teksten endres (UICulture). Tall og datoer leses fortsatt
// med punktum (InvariantCulture), slik at koordinatene fra kartet virker.
CultureInfo norsk = new CultureInfo("nb");
CultureInfo engelsk = new CultureInfo("en");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture, norsk),
    SupportedCultures = new List<CultureInfo> { CultureInfo.InvariantCulture },
    SupportedUICultures = new List<CultureInfo> { norsk, engelsk },
    RequestCultureProviders = new List<IRequestCultureProvider> { new CookieRequestCultureProvider() }
});

app.UseRouting();

app.UseAuthentication();

// Fartsgrensene (Services/Sikkerhet.cs). Står etter UseAuthentication,
// fordi grensen for nye behov og ressurser telles per innlogget bruker.
app.UseRateLimiter();

app.UseAuthorization();

// Gjør at bilder som lastes opp mens appen kjører (wwwroot/uploads) kan vises.
// MapStaticAssets under kjenner bare filene som fantes da appen ble bygget.
// (Sikkerhetshodene over gjelder også for disse filene.)
app.UseStaticFiles();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();