using System.ComponentModel.DataAnnotations;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for personvern (GDPR) og passordreglene.
public class PersonvernTests
{
    private static readonly DateTime Naa = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    // ===================================================
    // ----- Automatisk sletting av kontaktinfo (90 dager) -----
    // ===================================================
    [Theory]
    [InlineData(BehovStatus.Resolved, 100, true)]
    [InlineData(BehovStatus.Resolved, 10, false)]       // nyere enn grensen
    [InlineData(BehovStatus.Rejected, 91, true)]
    [InlineData(BehovStatus.Withdrawn, 91, true)]
    [InlineData(BehovStatus.UnderReview, 365, false)]   // aktivt: kommunen må kunne nå personen
    public void SkalFjerneKontaktinfo_BareForGamleAvsluttedeBehov(BehovStatus status, int dagerSiden, bool forventet)
    {
        var behov = new Behov { Status = status, SistEndretTid = Naa.AddDays(-dagerSiden) };

        Assert.Equal(forventet, PersonvernOpprydding.SkalFjerneKontaktinfo(behov, Naa, 90));
    }

    [Fact]
    public void BehovUtenEndring_BrukerOpprettetTid()
    {
        var behov = new Behov { Status = BehovStatus.Resolved, OpprettetTid = Naa.AddDays(-120), SistEndretTid = null };

        Assert.True(PersonvernOpprydding.SkalFjerneKontaktinfo(behov, Naa, 90));
    }

    // ===================================================
    // ----- Sletting av gamle invitasjonslenker (90 dager) -----
    // ===================================================
    [Theory]
    [InlineData(-100, null, true)]     // gikk ut for 100 dager siden, aldri brukt
    [InlineData(-10, null, false)]     // gikk ut nylig
    [InlineData(5, -100, true)]        // brukt for 100 dager siden
    [InlineData(5, null, false)]       // fortsatt gyldig
    public void SkalSletteInvitasjon_BareGamleOgFerdige(int utloperOmDager, int? bruktForDager, bool forventet)
    {
        var invitasjon = new Invitasjon { UtloperTid = Naa.AddDays(utloperOmDager) };
        if (bruktForDager != null)
        {
            invitasjon.BruktTid = Naa.AddDays(bruktForDager.Value);
        }

        Assert.Equal(forventet, PersonvernOpprydding.SkalSletteInvitasjon(invitasjon, Naa, 90));
    }

    // ===========================
    // ----- Passordregler -----
    // ===========================
    [Theory]
    [InlineData("NyttPass1!", "NyttPass1!", "")]                  // gyldig
    [InlineData("abcdefgh", "abcdefgh", "NyttPassord")]           // mangler stor bokstav, tall og spesialtegn
    [InlineData("NyttPass1!", "NyttPass2!", "BekreftPassord")]    // ulike passord
    public void EndrePassord_Validering(string nytt, string bekreft, string feltMedFeil)
    {
        var model = new EndrePassordViewModel
        {
            NaavarendePassord = "Gammelt1!",
            NyttPassord = nytt,
            BekreftPassord = bekreft
        };

        var feil = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), feil, validateAllProperties: true);

        if (feltMedFeil == "")
        {
            Assert.Empty(feil);
        }
        else
        {
            Assert.Single(feil);
            Assert.Contains(feltMedFeil, feil[0].MemberNames);
        }
    }
}
