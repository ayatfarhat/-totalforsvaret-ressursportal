using System.Text.RegularExpressions;
using Nabohjelp.Models;
using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for invitasjonslenkene som gir en rolle (Services/Invitasjonsregler.cs).
public class InvitasjonTests
{
    private static readonly DateTime Naa = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    // =========================
    // ----- Kode og hash -----
    // =========================
    [Fact]
    public void LagKode_Er43TegnSomKanStaIEnNettadresse()
    {
        string kode = Invitasjonsregler.LagKode();

        Assert.Equal(43, kode.Length);
        Assert.True(Regex.IsMatch(kode, "^[A-Za-z0-9_-]+$"));
    }

    [Fact]
    public void LagKode_GirNyKodeHverGang()
    {
        string forste = Invitasjonsregler.LagKode();
        string andre = Invitasjonsregler.LagKode();

        Assert.False(forste == andre);
    }

    [Fact]
    public void LagHash_SammeKodeGirSammeHash_OgKodenSynesIkke()
    {
        string hash = Invitasjonsregler.LagHash("abc123");

        Assert.Equal(hash, Invitasjonsregler.LagHash("abc123"));
        Assert.Equal(64, hash.Length);
        Assert.DoesNotContain("abc123", hash);
    }

    // =====================================
    // ----- Kan lenken brukes nå? -----
    // =====================================
    [Fact]
    public void SjekkGyldig_UkjentKode_GirFeil()
    {
        Assert.Contains("finnes ikke", Invitasjonsregler.SjekkGyldig(null, Naa));
    }

    [Fact]
    public void SjekkGyldig_BruktLenke_GirFeil()
    {
        var invitasjon = new Invitasjon { UtloperTid = Naa.AddDays(1), BruktTid = Naa.AddHours(-1) };

        Assert.Contains("allerede brukt", Invitasjonsregler.SjekkGyldig(invitasjon, Naa));
    }

    [Fact]
    public void SjekkGyldig_UtloptLenke_GirFeil()
    {
        var invitasjon = new Invitasjon { UtloperTid = Naa.AddMinutes(-1) };

        Assert.Contains("gått ut", Invitasjonsregler.SjekkGyldig(invitasjon, Naa));
    }

    [Fact]
    public void SjekkGyldig_NyLenke_ErGyldig()
    {
        var invitasjon = new Invitasjon { UtloperTid = Naa.AddDays(3) };

        Assert.Null(Invitasjonsregler.SjekkGyldig(invitasjon, Naa));
    }

    // =======================================
    // ----- Kan brukeren ta imot rollen? -----
    // =======================================
    [Fact]
    public void SjekkBruker_Admin_KanIkkeBrukeLenke()
    {
        // Ellers kunne en administrator mistet Admin-rollen ved et uhell.
        Assert.NotNull(Invitasjonsregler.SjekkBruker(new List<string> { "Admin" }, "Kommune"));
    }

    [Fact]
    public void SjekkBruker_HarAlleredeRollen_GirFeil()
    {
        Assert.NotNull(Invitasjonsregler.SjekkBruker(new List<string> { "Kommune" }, "Kommune"));
    }

    [Fact]
    public void SjekkBruker_Frivillig_KanBliKommune()
    {
        Assert.Null(Invitasjonsregler.SjekkBruker(new List<string> { "Frivillig" }, "Kommune"));
    }

    [Fact]
    public void Roller_FrivilligKanIkkeDelesUtMedLenke()
    {
        // Alle blir Frivillig når de registrerer seg, så det trengs ingen lenke for det.
        Assert.False(Invitasjonsregler.Roller.Contains("Frivillig"));
    }
}
