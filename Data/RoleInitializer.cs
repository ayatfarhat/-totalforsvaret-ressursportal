using Microsoft.AspNetCore.Identity;

namespace Nabohjelp.Data;

public static class RoleInitializer
{
    public static async Task InitializeAsync(
        RoleManager<IdentityRole> roleManager)
    {
        // NB: "PublicActor"/"Operator" er Torbjørn/Ayat sine rollenavn (brukt av
        // Behov-funksjonen og databaseoppsettet). "Admin"/"Kommune"/"Frivillig" er
        // Hassan sine rollenavn (brukt av innlogging/registrering, Kommandobro og
        // kartet). Begge sett må finnes helt til gruppa har blitt enige om ett
        // felles rollenavn-system — se oppgavefordelingen/gruppechat.
        string[] roles =
        {
            "PublicActor",
            "Operator",
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