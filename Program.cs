// Gjør at modeller og tjenester også kan vise tekst på valgt språk (se Services/Oversetter.cs).
Oversetter.Localizer = app.Services.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<SharedResource>>();

// Kryptering av personopplysninger i databasen (se Services/Kryptering.cs).
// «Personopplysninger» er formålet: nøkler for ett formål kan ikke dekryptere noe annet.
Kryptering.Beskytter = app.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("Personopplysninger");

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

    // Krypterer telefon, e-post og navn som ble lagret før krypteringen fantes.
    await Kryptering.KrypterGamleOpplysninger(db);
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

// I Docker-utvikling eksponeres appen på vanlig HTTP-port 8080 uten TLS.
// HTTPS-omdirigering er bare ønskelig i produksjon eller andre miljøer som faktisk
// har HTTPS aktivt. Uten denne sjekken ville appen sende 307 til https://... og
// systemtestene / helsesjekken ville feile når de prøver http://localhost:8080/.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Språket velges med knappen NO/EN og huskes i en cookie.
// Bare språket på teksten endres (UICulture). Tall og datoer leses fortsatt
// med punktum (InvariantCulture), slik at koordinatene fra kartet virker.
CultureInfo norsk = new CultureInfo("nb");
CultureInfo engelsk = new CultureInfo("en");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture, norsk),
    SupportedCultures = new List< CultureInfo > { CultureInfo.InvariantCulture },
    SupportedUICultures = new List< CultureInfo > { norsk, engelsk },
    RequestCultureProviders = new List<IRequestCultureProvider> { new CookieRequestCultureProvider() }
});
