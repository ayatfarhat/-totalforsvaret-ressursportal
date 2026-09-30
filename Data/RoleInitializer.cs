using Microsoft.AspNetCore.Identity;

namespace Nabohjelp.Data;

public static class RoleInitializer
{
    public static async Task InitializeAsync(
        RoleManager<IdentityRole> roleManager)
    {
        // Rollene brukt i resten av appen (innlogging/registrering, Kommandobro,
        // kartet og Behov). Erstatter de tidligere "PublicActor"/"Operator"-
        // navnene som Behov-funksjonen og databaseoppsettet opprinnelig brukte.
        string[] roles =
        {
            "Admin",
            "Kommune",
            "Frivillig"
        };

        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(
                    new IdentityRole(role)
                );
            }
        }
    }
}