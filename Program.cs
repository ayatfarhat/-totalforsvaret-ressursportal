using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Kriseportal.Data;

// Mappen for statiske filer (CSS, JS, bilder, biblioteker) heter "DesignCSS"
// i stedet for standardnavnet "wwwroot" — dette må settes med det samme,
// som en del av WebApplicationOptions, ikke etterpå med UseWebRoot().
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "DesignCSS"
});

// Database: SQLite lagrer alt i filen kriseportal.db (Hassans oppsett,
// for innlogging/roller). Torbjørns egen datamodell kobles til senere.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=kriseportal.db"));

// Identity: brukere, roller, passord og 2FA
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Passordkrav: minst 8 tegn og minst ett tall
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;

        // Låser kontoen i 5 minutter etter 5 feil passord
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); // trengs for 2FA

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Lager databasen, rollene og testbrukerne
await SeedData.LagRollerOgBrukere(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enkel, standard håndtering av statiske filer (CSS/JS/bilder) fra DesignCSS-mappen.
app.UseStaticFiles();

app.UseRouting();

// Først: hvem er brukeren? Så: hva har brukeren lov til?
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
