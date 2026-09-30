using Microsoft.AspNetCore.Identity;

namespace Kriseportal.Data;

// Kjøres når appen starter. Lager databasen, rollene og tre testbrukere.
public static class SeedData
{
    public static async Task LagRollerOgBrukere(WebApplication app)
    {
        // "using" her betyr: rydd opp automatisk når vi er ferdige i denne blokken.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // Lager databasefilen hvis den ikke finnes
            await db.Database.EnsureCreatedAsync();

            // Lager de tre rollene
            string[] roller = { "Admin", "Kommune", "Frivillig" };
            foreach (var rolle in roller)
            {
                if (!await roleManager.RoleExistsAsync(rolle))
                {
                    await roleManager.CreateAsync(new IdentityRole(rolle));
                }
            }

            // Testbrukere. Bare for testing!
            await LagBruker(userManager, "admin@test.no", "Admin123", "Admin");
            await LagBruker(userManager, "kommune@test.no", "Kommune123", "Kommune");
            await LagBruker(userManager, "privatperson@test.no", "Privat123", "Frivillig");
        }
    }

    private static async Task LagBruker(UserManager<IdentityUser> userManager, string epost, string passord, string rolle)
    {
        // Hopper over hvis brukeren allerede finnes
        if (await userManager.FindByEmailAsync(epost) != null)
        {
            return;
        }

        var bruker = new IdentityUser { UserName = epost, Email = epost };
        await userManager.CreateAsync(bruker, passord);
        await userManager.AddToRoleAsync(bruker, rolle);
    }
}
