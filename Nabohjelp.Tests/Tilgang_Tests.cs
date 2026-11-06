using System.Security.Claims;
using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

// Tester regelen for hvem som er ansatt (Admin/Kommune), med og uten krav om 2FA.
public class TilgangTests
{
    private static ClaimsPrincipal Bruker(string rolle, bool medTofa)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.Role, rolle) };
        if (medTofa)
        {
            claims.Add(new Claim("amr", "mfa"));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public void UtenKravOmTofa_ErKommuneAnsatt()
    {
        Tilgang.KrevTofaForAnsatte = false;

        Assert.True(Tilgang.ErAnsatt(Bruker("Kommune", medTofa: false)));
        Assert.False(Tilgang.ErAnsatt(Bruker("Frivillig", medTofa: false)));
    }

    [Fact]
    public void MedKravOmTofa_MaaKommuneHaTofa()
    {
        Tilgang.KrevTofaForAnsatte = true;

        Assert.False(Tilgang.ErAnsatt(Bruker("Kommune", medTofa: false)));
        Assert.True(Tilgang.ErAnsatt(Bruker("Kommune", medTofa: true)));
        Assert.False(Tilgang.ErAdmin(Bruker("Kommune", medTofa: true)));
        Assert.True(Tilgang.ErAdmin(Bruker("Admin", medTofa: true)));

        Tilgang.KrevTofaForAnsatte = false;
    }
}
