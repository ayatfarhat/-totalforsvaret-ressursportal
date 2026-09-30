using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Data;

var builder = WebApplication.CreateBuilder(args);

// Legger til MVC
builder.Services.AddControllersWithViews();

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
// og oppretter rollene PublicActor og Operator
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

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();