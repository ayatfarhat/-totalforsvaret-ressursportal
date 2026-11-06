using Microsoft.AspNetCore.Identity;

namespace Nabohjelp.Data;

// Oppretter rollene når appen starter (se Program.cs), hvis de ikke finnes fra før.
public static class RoleInitializer
{
    // Alle rollene i systemet. Brukes også av AdminController når admin bytter rolle.
    public static readonly string[] Roller = { "Admin", "Kommune", "Frivillig" };

    public static async Task InitializeAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (string rolle in Roller)
        {
            if (!await roleManager.RoleExistsAsync(rolle))
            {
                await roleManager.CreateAsync(new IdentityRole(rolle));
            }
        }
    }
}
