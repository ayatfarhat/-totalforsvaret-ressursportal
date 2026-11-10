using Microsoft.AspNetCore.DataProtection;
using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for krypteringen av personopplysninger (Services/Kryptering.cs)
// og knappene for innlogging med Vipps og Apple (Services/EksternInnlogging.cs).
public class KrypteringTests
{
    // En beskytter med nøkler som bare finnes mens testen kjører.
    private static IDataProtector LagBeskytter()
    {
        return new EphemeralDataProtectionProvider().CreateProtector("Personopplysninger");
    }

    [Fact]
    public void Krypter_Telefon_LagresIkkeSomVanligTekst()
    {
        string? kryptert = Kryptering.Krypter("91234567", LagBeskytter());

        Assert.NotNull(kryptert);
        Assert.StartsWith(Kryptering.Merke, kryptert);
        Assert.DoesNotContain("91234567", kryptert);
    }

    [Fact]
    public void KrypterOgDekrypter_GirSammeTekstTilbake()
    {
        IDataProtector beskytter = LagBeskytter();

        string? kryptert = Kryptering.Krypter("ola@test.no", beskytter);
        string? dekryptert = Kryptering.Dekrypter(kryptert, beskytter);

        Assert.Equal("ola@test.no", dekryptert);
    }

    [Fact]
    public void Krypter_SammeTekstToGanger_GirUlikKryptertTekst()
    {
        // Ellers kunne man se hvem som har samme telefonnummer ved å sammenligne radene.
        IDataProtector beskytter = LagBeskytter();

        Assert.NotEqual(Kryptering.Krypter("91234567", beskytter), Kryptering.Krypter("91234567", beskytter));
    }

    [Fact]
    public void Krypter_AlleredeKryptert_KrypteresIkkePaNytt()
    {
        IDataProtector beskytter = LagBeskytter();
        string? kryptert = Kryptering.Krypter("91234567", beskytter);

        Assert.Equal(kryptert, Kryptering.Krypter(kryptert, beskytter));
    }

    [Fact]
    public void Dekrypter_GammelUkryptertVerdi_VisesSomFor()
    {
        // Verdier fra før krypteringen fantes, skal fortsatt kunne leses.
        Assert.Equal("91234567", Kryptering.Dekrypter("91234567", LagBeskytter()));
    }

    [Fact]
    public void KrypterOgDekrypter_TomtFelt_ForblirTomt()
    {
        IDataProtector beskytter = LagBeskytter();

        Assert.Null(Kryptering.Krypter(null, beskytter));
        Assert.Null(Kryptering.Dekrypter(null, beskytter));
    }

    [Fact]
    public void Dekrypter_MedFeilNokkel_KrasjerIkke()
    {
        // Kryptert med én nøkkel og lest med en annen (for eksempel hvis nøklene er borte).
        string? kryptert = Kryptering.Krypter("91234567", LagBeskytter());

        string? resultat = Kryptering.Dekrypter(kryptert, LagBeskytter());

        Assert.Equal("(kan ikke vises)", resultat);
    }

    [Fact]
    public void ErKryptert_SkillerKryptertFraVanligTekst()
    {
        Assert.True(Kryptering.ErKryptert(Kryptering.Krypter("91234567", LagBeskytter())));
        Assert.False(Kryptering.ErKryptert("91234567"));
        Assert.False(Kryptering.ErKryptert(null));
    }

    [Theory]
    [InlineData("Vipps", true)]
    [InlineData("Apple", true)]
    [InlineData("Google", false)]
    [InlineData("", false)]
    public void EksternInnlogging_GodtarBareVippsOgApple(string leverandor, bool forventet)
    {
        Assert.Equal(forventet, EksternInnlogging.Finnes(leverandor));
    }

    [Fact]
    public void EksternInnlogging_UtenNokler_ErIkkeAktiv()
    {
        // Testene har ingen nøkler fra Vipps eller Apple, så ingen av dem er slått på.
        Assert.False(EksternInnlogging.ErAktiv("Vipps"));
        Assert.False(EksternInnlogging.ErAktiv("Apple"));
    }
}
