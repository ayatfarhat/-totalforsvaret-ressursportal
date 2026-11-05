using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Nabohjelp;
using Nabohjelp.Data;
using Nabohjelp.Services;

// Tall (som koordinatene fra kartet) skal alltid leses med punktum som
// desimaltegn, uansett hvilket språk maskinen som kjører appen bruker.
// Uten denne kunne f.eks. "64.5" blitt feiltolket på en norsk maskin.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Språkvalg: tekstene på engelsk ligger i mappen Resources.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Legger til MVC
builder.Services.AddControllersWithViews(options =>
{
    // Norske feilmeldinger når en verdi ikke kan leses (for eksempel bokstaver i et tallfelt).
    var meldinger = options.ModelBindingMessageProvider;
    meldinger.SetAttemptedValueIsInvalidAccessor((verdi, felt) => $"«{verdi}» er ikke en gyldig verdi for {felt}.");
    meldinger.SetValueIsInvalidAccessor(verdi => $"«{verdi}» er ikke en gyldig verdi.");
    meldinger.SetValueMustBeANumberAccessor(felt => $"{felt} må være et tall.");
    meldinger.SetValueMustNotBeNullAccessor(verdi => "Feltet må fylles ut.");
    meldinger.SetMissingKeyOrValueAccessor(() => "Feltet må fylles ut.");
    meldinger.SetUnknownValueIsInvalidAccessor(felt => $"Verdien er ikke gyldig for {felt}.");
    meldinger.SetNonPropertyAttemptedValueIsInvalidAccessor(verdi => $"«{verdi}» er ikke en gyldig verdi.");
    meldinger.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Verdien er ikke gyldig.");
    meldinger.SetNonPropertyValueMustBeANumberAccessor(() => "Feltet må være et tall.");
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
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

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
    options.OnRefreshingPrincipal = context =>
    {
        var amr = context.CurrentPrincipal?.FindFirst("amr");
        if (amr != null && context.NewPrincipal?.Identity is ClaimsIdentity ny && !ny.HasClaim("amr", amr.Value))
        {
            ny.AddClaim(new Claim("amr", amr.Value));
        }
        return Task.CompletedTask;
    };
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

// Ulike brukergrupper har ulike krav til innlogging.
// Admin og Kommune har tilgang til Kommandobroen og personopplysninger om andre.
// Når "KrevTofaForAnsatte" er true i appsettings, må de i tillegg ha logget inn med 2FA.
// Identity legger til "amr" = "mfa" i innloggingen når brukeren har brukt 2FA.
bool krevTofa = builder.Configuration.GetValue<bool>("Sikkerhet:KrevTofaForAnsatte");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Ansatt", policy =>
    {
        policy.RequireRole("Admin", "Kommune");
        if (krevTofa)
        {
            policy.RequireClaim("amr", "mfa");
        }
    });

    options.AddPolicy("Admin", policy =>
    {
        policy.RequireRole("Admin");
        if (krevTofa)
        {
            policy.RequireClaim("amr", "mfa");
        }
    });
});

// Sletting og nedlasting av brukerdata (brukes av AccountController og AdminController).
builder.Services.AddScoped<KontoService>();

var app = builder.Build();

// Kjører migrasjoner automatisk
// og oppretter rollene Admin, Kommune og Frivillig
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    await RoleInitializer.InitializeAsync(roleManager);
}

// Lager testbrukere etter at databasen og rollene finnes
await SeedData.LagTestbrukere(app);

// GDPR: fjerner kontaktopplysninger fra gamle, avsluttede behov.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    int dager = app.Configuration.GetValue("Personvern:SlettKontaktinfoEtterDager", 90);
    await PersonvernOpprydding.FjernGamleKontaktopplysninger(db, dager);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Viser en norsk side ved 404 og andre feilkoder i stedet for en tom side.
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

app.UseHttpsRedirection();

// Språket velges med knappen NO/EN og huskes i en cookie.
// Bare språket på teksten endres (UICulture). Tall og datoer leses fortsatt
// med punktum (InvariantCulture), slik at koordinatene fra kartet virker.
var norsk = new CultureInfo("nb");
var engelsk = new CultureInfo("en");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture, norsk),
    SupportedCultures = new List<CultureInfo> { CultureInfo.InvariantCulture },
    SupportedUICultures = new List<CultureInfo> { norsk, engelsk },
    RequestCultureProviders = new List<IRequestCultureProvider> { new CookieRequestCultureProvider() }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Gjør at bilder som lastes opp mens appen kjører (wwwroot/uploads) kan vises.
// MapStaticAssets under kjenner bare filene som fantes da appen ble bygget.
app.UseStaticFiles(new StaticFileOptions
{
    // Forteller nettleseren at den ikke skal «gjette» filtypen på det som er lastet opp.
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    }
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();