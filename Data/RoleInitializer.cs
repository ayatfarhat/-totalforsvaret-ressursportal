using Microsoft.AspNetCore.Identity;

namespace Nabohjelp.Data;

public static class RoleInitializer
{
    public static async Task InitializeAsync(
        RoleManager<IdentityRole> roleManager)
    {
        string[] roles =
        {
            "PublicActor",
            "Operator"
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