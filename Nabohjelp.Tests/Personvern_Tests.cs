using System.ComponentModel.DataAnnotations;
using Nabohjelp.Models;
using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for personvern (GDPR) og kontoskjemaene.
public class PersonvernTests
{
    private static readonly DateTime Naa = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    // Lager et behov som sist ble endret for et gitt antall dager siden.
    private static Behov LagBehov(BehovStatus status, int dagerSiden)
    {
        return new Behov
        {
            Status = status,
            KontaktTelefon = "12345678",
            KontaktEpost = "ola@test.no",
            OpprettetTid = Naa.AddDays(-dagerSiden),
            SistEndretTid = Naa.AddDays(-dagerSiden)
        };
    }

    // Kjører valideringen i en ViewModel og returnerer feilene.
    private static List<ValidationResult> Valider(object model)
    {
        var feil = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), feil, validateAllProperties: true);
        return feil;
    }

    // ======================================
    // ----- Automatisk sletting av kontaktinfo -----
    // ======================================
    [Fact]
    public void LostBehov_EldreEnnGrensen_KontaktinfoFjernes()
    {
        var behov = LagBehov(BehovStatus.Resolved, dagerSiden: 100);

        Assert.True(PersonvernOpprydding.SkalFjerneKontaktinfo(behov, Naa, 90));
    }

    [Fact]
    public void LostBehov_NyereEnnGrensen_KontaktinfoBeholdes()
    {
        var behov = LagBehov(BehovStatus.Resolved, dagerSiden: 10);

        Assert.False(PersonvernOpprydding.SkalFjerneKontaktinfo(behov, Naa, 90));
    }

    [Fact]
    public void AktivtBehov_KontaktinfoBeholdesUansettAlder()
    {
        // Kommunen må kunne nå personen så lenge behovet ikke er avsluttet.
        var behov = LagBehov(BehovStatus.UnderReview, dagerSiden: 365);

        Assert.False(PersonvernOpprydding.SkalFjerneKontaktinfo(behov, Naa, 90));
    }

    [Theory]
    [InlineData(BehovStatus.Rejected)]
    [InlineData(BehovStatus.Withdrawn)]
    public void AvvistEllerTrukketBehov_RegnesSomAvsluttet(BehovStatus status)
    {
        var behov = LagBehov(status, dagerSiden: 91);

        Assert.True(PersonvernOpprydding.SkalFjerneKontaktinfo(behov, Naa, 90));
    }

    [Fact]
    public void BehovUtenEndring_BrukerOpprettetTid()
    {
        var behov = LagBehov(BehovStatus.Resolved, dagerSiden: 120);
        behov.SistEndretTid = null;

        Assert.True(PersonvernOpprydding.SkalFjerneKontaktinfo(behov, Naa, 90));
    }

    // ======================================
    // ----- Passordregler -----
    // ======================================
    [Fact]
    public void EndrePassord_SvaktPassord_GirFeil()
    {
        var model = new EndrePassordViewModel
        {
            NaavarendePassord = "Gammelt1!",
            NyttPassord = "abcdefgh",
            BekreftPassord = "abcdefgh"
        };

        Assert.Contains(Valider(model), f => f.MemberNames.Contains(nameof(EndrePassordViewModel.NyttPassord)));
    }

    [Fact]
    public void EndrePassord_UlikeNyePassord_GirFeil()
    {
        var model = new EndrePassordViewModel
        {
            NaavarendePassord = "Gammelt1!",
            NyttPassord = "NyttPass1!",
            BekreftPassord = "NyttPass2!"
        };

        Assert.Contains(Valider(model), f => f.MemberNames.Contains(nameof(EndrePassordViewModel.BekreftPassord)));
    }

    [Fact]
    public void EndrePassord_GyldigSkjema_HarIngenFeil()
    {
        var model = new EndrePassordViewModel
        {
            NaavarendePassord = "Gammelt1!",
            NyttPassord = "NyttPass1!",
            BekreftPassord = "NyttPass1!"
        };

        Assert.Empty(Valider(model));
    }
}
