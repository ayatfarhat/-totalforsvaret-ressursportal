using System.Text.RegularExpressions;
using Nabohjelp.Models;
using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for invitasjonslenkene som gir en rolle (Services/Invitasjonsregler.cs).
public class InvitasjonTests
{
    private static readonly DateTime Naa = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void LagKode_GirNyKodeSomKanStaIEnNettadresse()
    {
        string kode = Invitasjonsregler.LagKode();

        Assert.Equal(43, kode.Length);
        Assert.True(Regex.IsMatch(kode, "^[A-Za-z0-9_-]+$"));
        Assert.False(kode == Invitasjonsregler.LagKode());
    }

    [Fact]
    public void LagHash_SammeKodeGirSammeHash_OgKodenSynesIkke()
    {
        string hash = Invitasjonsregler.LagHash("abc123");

        Assert.Equal(hash, Invitasjonsregler.LagHash("abc123"));
        Assert.Equal(64, hash.Length);
        Assert.DoesNotContain("abc123", hash);
    }

    [Fact]
    public void SjekkGyldig_BareNyOgUbruktLenkeVirker()
    {
        var brukt = new Invitasjon { UtloperTid = Naa.AddDays(1), BruktTid = Naa.AddHours(-1) };
        var utlopt = new Invitasjon { UtloperTid = Naa.AddMinutes(-1) };
        var ny = new Invitasjon { UtloperTid = Naa.AddDays(3) };

        Assert.Contains("finnes ikke", Invitasjonsregler.SjekkGyldig(null, Naa));
        Assert.Contains("allerede brukt", Invitasjonsregler.SjekkGyldig(brukt, Naa));
        Assert.Contains("gått ut", Invitasjonsregler.SjekkGyldig(utlopt, Naa));
        Assert.Null(Invitasjonsregler.SjekkGyldig(ny, Naa));
    }

    [Theory]
    [InlineData("Frivillig", "Kommune", true)]
    [InlineData("Kommune", "Kommune", false)]   // har allerede rollen
    [InlineData("Admin", "Kommune", false)]     // en administrator skal ikke miste Admin ved et uhell
    public void SjekkBruker_HvemKanTaImotRollen(string rolleNaa, string nyRolle, bool kanTaImot)
    {
        string? feil = Invitasjonsregler.SjekkBruker(new List<string> { rolleNaa }, nyRolle);

        Assert.Equal(kanTaImot, feil == null);
    }
}
