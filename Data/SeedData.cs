using Microsoft.AspNetCore.Identity;

namespace Nabohjelp.Data;

// Lager testbrukere ved oppstart.
// Bare ment for utvikling og testing.
public static class SeedData
{
    public static async Task LagTestbrukere(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var userManager =
            scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await LagBruker(
            userManager,
            "operator@test.no",
            "Operator123!",
            "Operator"
        );

        await LagBruker(
            userManager,
            "public@test.no",
            "Public123!",
            "PublicActor"
        );

        // Testbrukere for Hassan sine rollenavn (innlogging, Kommandobro, kartet).
        // Se merknaden i RoleInitializer.cs - to rollenavn-systemer lever side om
        // side inntil gruppa er enige om ett felles.
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
        var bruker = await userManager.FindByEmailAsync(epost);

        if (bruker != null)
        {
            return;
        }

        bruker = new IdentityUser
        {
            UserName = epost,
            Email = epost
        };

        var resultat =
            await userManager.CreateAsync(bruker, passord);

        if (resultat.Succeeded)
        {
            await userManager.AddToRoleAsync(bruker, rolle);
        }
    }
}