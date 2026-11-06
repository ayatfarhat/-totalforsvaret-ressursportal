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

    // En innlogget frivillig med ID og e-post.
    private static ClaimsPrincipal Frivillig(string id)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Name, id + "@test.no"),
            new Claim(ClaimTypes.Role, "Frivillig")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public void Eier_KanEndreEgenRegistrering_MenIkkeAndres()
    {
        Tilgang.KrevTofaForAnsatte = false;
        ClaimsPrincipal ola = Frivillig("ola");

        Assert.True(Tilgang.ErEier(ola, "ola"));
        Assert.True(Tilgang.KanEndre(ola, "ola"));
        Assert.False(Tilgang.KanEndre(ola, "kari"));
    }

    [Fact]
    public void Kommune_KanEndreAndresRegistrering()
    {
        Tilgang.KrevTofaForAnsatte = false;

        Assert.True(Tilgang.KanEndre(Bruker("Kommune", medTofa: false), "kari"));
    }

    [Fact]
    public void IkkeInnlogget_ErIkkeEierAvNoe()
    {
        var anonym = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.False(Tilgang.ErInnlogget(anonym));
        Assert.False(Tilgang.ErEier(anonym, null));
        Assert.Equal("", Tilgang.Epost(anonym));
    }

    [Fact]
    public void Innlogget_HarIdOgEpost()
    {
        ClaimsPrincipal ola = Frivillig("ola");

        Assert.True(Tilgang.ErInnlogget(ola));
        Assert.Equal("ola", Tilgang.BrukerId(ola));
        Assert.Equal("ola@test.no", Tilgang.Epost(ola));
    }
}
