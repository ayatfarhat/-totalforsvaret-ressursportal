using System.ComponentModel.DataAnnotations;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for statusflyten til behov (BehovStatusflyt) og for behovsskjemaet (BehovViewModel).
public class BehovStatusflytTests
{
    // ======================================
    // ----- Lovlige og ulovlige statusendringer -----
    // ======================================
    [Fact]
    public void NyttBehov_KanVurderesEllerAvvises()
    {
        var neste = BehovStatusflyt.NesteStatuser(BehovStatus.New);

        Assert.Equal(new List<BehovStatus> { BehovStatus.UnderReview, BehovStatus.Rejected }, neste);
    }

    [Fact]
    public void NyttBehov_KanIkkeHoppeRettTilLost()
    {
        Assert.False(BehovStatusflyt.KanEndres(BehovStatus.New, BehovStatus.Resolved));
    }

    [Fact]
    public void UnderVurdering_KanLosesEllerAvvises()
    {
        Assert.True(BehovStatusflyt.KanEndres(BehovStatus.UnderReview, BehovStatus.Resolved));
        Assert.True(BehovStatusflyt.KanEndres(BehovStatus.UnderReview, BehovStatus.Rejected));
    }

    [Fact]
    public void Tildelt_KanBareLoses()
    {
        Assert.Equal(new List<BehovStatus> { BehovStatus.Resolved }, BehovStatusflyt.NesteStatuser(BehovStatus.Assigned));
        Assert.False(BehovStatusflyt.KanEndres(BehovStatus.Assigned, BehovStatus.Rejected));
    }

    [Fact]
    public void Tildelt_SettesIkkeMedKnapp()
    {
        // «Tildelt» settes bare av tildelingen, aldri direkte fra køen.
        Assert.False(BehovStatusflyt.KanEndres(BehovStatus.New, BehovStatus.Assigned));
        Assert.False(BehovStatusflyt.KanEndres(BehovStatus.UnderReview, BehovStatus.Assigned));
    }

    [Fact]
    public void AvsluttedeBehov_KanIkkeEndresMer()
    {
        Assert.Empty(BehovStatusflyt.NesteStatuser(BehovStatus.Resolved));
        Assert.Empty(BehovStatusflyt.NesteStatuser(BehovStatus.Rejected));
        Assert.Empty(BehovStatusflyt.NesteStatuser(BehovStatus.Withdrawn));
    }

    // ==========================
    // ----- Trekke tilbake -----
    // ==========================
    [Fact]
    public void TrekkTilbake_GarForTildeling()
    {
        Assert.True(BehovStatusflyt.KanTrekkesTilbake(BehovStatus.New));
        Assert.True(BehovStatusflyt.KanTrekkesTilbake(BehovStatus.UnderReview));
    }

    [Fact]
    public void TrekkTilbake_GarIkkeEtterTildeling()
    {
        Assert.False(BehovStatusflyt.KanTrekkesTilbake(BehovStatus.Assigned));
        Assert.False(BehovStatusflyt.KanTrekkesTilbake(BehovStatus.Resolved));
        Assert.False(BehovStatusflyt.KanTrekkesTilbake(BehovStatus.Rejected));
        Assert.False(BehovStatusflyt.KanTrekkesTilbake(BehovStatus.Withdrawn));
    }

    // ===============================
    // ----- Avsluttet og statuslinje -----
    // ===============================
    [Fact]
    public void ErAvsluttet_GjelderLostAvvistOgTrukket()
    {
        Assert.True(BehovStatusflyt.ErAvsluttet(BehovStatus.Resolved));
        Assert.True(BehovStatusflyt.ErAvsluttet(BehovStatus.Rejected));
        Assert.True(BehovStatusflyt.ErAvsluttet(BehovStatus.Withdrawn));
        Assert.False(BehovStatusflyt.ErAvsluttet(BehovStatus.New));
        Assert.False(BehovStatusflyt.ErAvsluttet(BehovStatus.Assigned));
    }

    [Fact]
    public void StegNummer_FolgerStatuslinjen()
    {
        Assert.Equal(0, BehovStatusflyt.StegNummer(BehovStatus.New));
        Assert.Equal(1, BehovStatusflyt.StegNummer(BehovStatus.UnderReview));
        Assert.Equal(2, BehovStatusflyt.StegNummer(BehovStatus.Assigned));
        Assert.Equal(3, BehovStatusflyt.StegNummer(BehovStatus.Resolved));
        Assert.Equal(-1, BehovStatusflyt.StegNummer(BehovStatus.Rejected));
    }

    [Fact]
    public void NyeStatuser_HarNorskTekst()
    {
        Assert.Equal("Avvist", Visningstekst.Status(BehovStatus.Rejected));
        Assert.Equal("Trukket tilbake", Visningstekst.Status(BehovStatus.Withdrawn));
    }

    // ===========================
    // ----- Behovsskjemaet -----
    // ===========================
    [Fact]
    public void Telefon_LagresMedLandskode()
    {
        var skjema = new BehovViewModel { Landskode = "+46", KontaktTelefon = "70 123 45 67" };

        Assert.Equal("+46 70 123 45 67", skjema.TelefonForLagring());
    }

    [Fact]
    public void Telefon_TomLagresSomIngenting()
    {
        var skjema = new BehovViewModel { KontaktTelefon = "" };

        Assert.Null(skjema.TelefonForLagring());
    }

    [Fact]
    public void Autofyll_GirForslagForTransport()
    {
        Assert.StartsWith("Hva skal fraktes", BehovViewModel.HentForslag(BehovType.Transport));
    }

    [Fact]
    public void Autofyll_AnnetGirTomTekst()
    {
        Assert.Equal("", BehovViewModel.HentForslag(BehovType.Annet));
    }

    [Fact]
    public void Skjema_UtenTelefonOgEpost_ErUgyldig()
    {
        var skjema = GyldigSkjema();
        skjema.KontaktTelefon = null;
        skjema.KontaktEpost = null;

        Assert.False(ErGyldig(skjema));
    }

    [Fact]
    public void Skjema_UkjentLandskode_ErUgyldig()
    {
        var skjema = GyldigSkjema();
        skjema.Landskode = "+999";

        Assert.False(ErGyldig(skjema));
    }

    [Fact]
    public void Skjema_TelefonMedBokstaver_ErUgyldig()
    {
        var skjema = GyldigSkjema();
        skjema.KontaktTelefon = "ring meg";

        Assert.False(ErGyldig(skjema));
    }

    [Fact]
    public void Skjema_GyldigUtfylt_ErGyldig()
    {
        Assert.True(ErGyldig(GyldigSkjema()));
    }

    private static BehovViewModel GyldigSkjema()
    {
        return new BehovViewModel
        {
            Type = BehovType.Generator,
            Prioritet = BehovPrioritet.Urgent,
            Latitude = 58.34,
            Longitude = 8.59,
            Landskode = "+47",
            KontaktTelefon = "41234567",
            KontaktEpost = "kari@example.no"
        };
    }

    // Kjører de samme valideringsreglene som ASP.NET bruker på skjemaet,
    // også Validate-metoden (telefon eller e-post, landskode).
    private static bool ErGyldig(BehovViewModel modell)
    {
        var resultater = new List<ValidationResult>();
        return Validator.TryValidateObject(
            modell,
            new ValidationContext(modell),
            resultater,
            validateAllProperties: true);
    }
}
