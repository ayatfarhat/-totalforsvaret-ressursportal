using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;

// Tall (som koordinatene fra kartet) skal alltid leses med punktum som
// desimaltegn, uansett hvilket språk maskinen som kjører appen bruker.
// Uten denne kunne f.eks. "64.5" blitt feiltolket på en norsk maskin.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

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
    .AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Viser en norsk side ved 404 og andre feilkoder i stedet for en tom side.
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

app.UseHttpsRedirection();
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