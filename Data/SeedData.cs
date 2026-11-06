using Microsoft.AspNetCore.Identity;

namespace Nabohjelp.Data;

// Lager testbrukere ved oppstart.
// Bare ment for utvikling og testing.
public static class SeedData
{
    public static async Task LagTestbrukere(WebApplication app)
    {
        // Henter UserManager (Identity sin klasse for brukere) fra appens tjenester.
        // «using» rydder opp etter oss når metoden er ferdig.
        using IServiceScope scope = app.Services.CreateScope();
        UserManager<IdentityUser> userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await LagBruker(
            userManager,
            "admin@test.no",
            "Admin123!",
            "Admin"
        );

        await LagBruker(
            userManager,
            "kommune@test.no",
            "Kommune123!",
            "Kommune"
        );

        await LagBruker(
            userManager,
            "privatperson@test.no",
            "Privat123!",
            "Frivillig"
        );
    }

    private static async Task LagBruker(
        UserManager<IdentityUser> userManager,
        string epost,
        string passord,
        string rolle)
    {
        // Finnes brukeren fra før (appen er startet før), gjør vi ingenting.
        IdentityUser? finnes = await userManager.FindByEmailAsync(epost);

        if (finnes != null)
        {
            return;
        }

        var bruker = new IdentityUser
        {
            UserName = epost,
            Email = epost
        };

        IdentityResult resultat = await userManager.CreateAsync(bruker, passord);

        if (resultat.Succeeded)
        {
            await userManager.AddToRoleAsync(bruker, rolle);
        }
    }
}