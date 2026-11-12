using System.ComponentModel.DataAnnotations;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using Xunit;
namespace Nabohjelp.Tests;
public class BehovStatusflytTests
{
    [Theory]
    [InlineData(BehovStatus.New, BehovStatus.UnderReview, true)]
    [InlineData(BehovStatus.New, BehovStatus.Rejected, true)]
    [InlineData(BehovStatus.New, BehovStatus.Resolved, false)]
    [InlineData(BehovStatus.New, BehovStatus.Assigned, false)]
    [InlineData(BehovStatus.UnderReview, BehovStatus.Resolved, true)]
    [InlineData(BehovStatus.UnderReview, BehovStatus.Rejected, true)]
    [InlineData(BehovStatus.UnderReview, BehovStatus.Assigned, false)]
    [InlineData(BehovStatus.Assigned, BehovStatus.Resolved, true)]
    [InlineData(BehovStatus.Assigned, BehovStatus.Rejected, false)]
    [InlineData(BehovStatus.Resolved, BehovStatus.New, false)]
    [InlineData(BehovStatus.Rejected, BehovStatus.UnderReview, false)]
    [InlineData(BehovStatus.Withdrawn, BehovStatus.New, false)]
    public void KanEndres_FolgerStatusflyten(BehovStatus fra, BehovStatus til, bool forventet)
    {
        Assert.Equal(forventet, BehovStatusflyt.KanEndres(fra, til));
    }
    [Theory]
    [InlineData(BehovStatus.New, true, false, 0)]
    [InlineData(BehovStatus.UnderReview, true, false, 1)]
    [InlineData(BehovStatus.Assigned, false, false, 2)]
    [InlineData(BehovStatus.Resolved, false, true, 3)]
    [InlineData(BehovStatus.Rejected, false, true, -1)]   // -1: ikke et steg i statuslinjen
    [InlineData(BehovStatus.Withdrawn, false, true, -1)]
    public void Status_GirRiktigeRegler(BehovStatus status, bool kanRedigeres, bool erAvsluttet, int steg)
    {
        Assert.Equal(kanRedigeres, BehovStatusflyt.KanRedigeres(status));
        Assert.Equal(erAvsluttet, BehovStatusflyt.ErAvsluttet(status));
        Assert.Equal(steg, BehovStatusflyt.StegNummer(status));
    }
    [Fact]
    public void FraBehov_FyllerSkjemaetOgDelerOppTelefonen()
    {
        var behov = new Behov
        {
            Id = 7,
            Type = BehovType.Transport,
            Prioritet = BehovPrioritet.Planned,
            Latitude = 58.2,
            Longitude = 8.1,
            KontaktTelefon = "+46 70 123 45 67"
        };
        BehovViewModel skjema = BehovViewModel.FraBehov(behov);

        Assert.Equal(7, skjema.Id);
        Assert.Equal(BehovType.Transport, skjema.Type);
        Assert.Equal("+46", skjema.Landskode);
        Assert.Equal("70 123 45 67", skjema.KontaktTelefon);
        Assert.Equal("+46 70 123 45 67", skjema.TelefonForLagring());
    }

    [Theory]
    [InlineData("41234567", "+47", "41234567")]     // gammelt nummer uten landskode: Norge
    [InlineData("+999 1234", "+47", "+999 1234")]   // ukjent landskode: nummeret forsvinner ikke
    public void LesTelefon_UtenKjentLandskode(string lagret, string landskode, string nummer)
    {
        var skjema = new BehovViewModel();

        skjema.LesTelefonFraLagring(lagret);

        Assert.Equal(landskode, skjema.Landskode);
        Assert.Equal(nummer, skjema.KontaktTelefon);
    }
    [Fact]
    public void KopierTil_LagrerAnnetTekstBareNaarTypenErAnnet()
    {
        var behov = new Behov();
        BehovViewModel skjema = GyldigSkjema();
        skjema.Type = BehovType.Annet;
        skjema.AnnetType = " steinras ";

        skjema.KopierTil(behov);
        Assert.Equal("steinras", behov.AnnetType);
        Assert.Equal("+47 41234567", behov.KontaktTelefon);

        skjema.Type = BehovType.Transport;
        skjema.KopierTil(behov);
        Assert.Null(behov.AnnetType);
    }

    [Fact]
    public void KopierTil_LagrerKoordinaterOgKommuneForBehovskøen()
    {
        var behov = new Behov();
        BehovViewModel skjema = GyldigSkjema();
        skjema.Latitude = 58.1467;
        skjema.Longitude = 7.9956;
        skjema.KommuneNavn = "Kristiansand";

        skjema.KopierTil(behov);

        Assert.Equal(58.1467, behov.Latitude, 5);
        Assert.Equal(7.9956, behov.Longitude, 5);
        Assert.Equal("4204", behov.Kommunenummer);

        BehovViewModel redigert = BehovViewModel.FraBehov(behov);
        Assert.Equal(58.1467, redigert.Latitude);
        Assert.Equal(7.9956, redigert.Longitude);
        Assert.Equal("Kristiansand", redigert.KommuneNavn);
    }
    [Theory]
    [InlineData(-90, -180, true)]
    [InlineData(90, 180, true)]
    [InlineData(90.01, 0, false)]
    [InlineData(0, 180.01, false)]
    public void Koordinater_ValideresInnenforGyldigeGrenser(double bredde, double lengde, bool forventet)
    {
        BehovViewModel skjema = GyldigSkjema();
        skjema.Latitude = bredde;
        skjema.Longitude = lengde;

        Assert.Equal(forventet, ErGyldig(skjema));
    }

    [Fact]
    public void Forslag_FinnesForTransport_MenIkkeForAnnet()
    {
        Assert.StartsWith("Hva skal fraktes", BehovViewModel.HentForslag(BehovType.Transport));
        Assert.Equal("", BehovViewModel.HentForslag(BehovType.Annet));
    }

    [Theory]
    [InlineData("ok", true)]
    [InlineData("uten kontakt", false)]
    [InlineData("ukjent landskode", false)]
    [InlineData("bokstaver i telefon", false)]
    [InlineData("annet uten tekst", false)]
    public void Skjema_Validering(string tilfelle, bool forventet)
    {
        BehovViewModel skjema = GyldigSkjema();
        if (tilfelle == "uten kontakt")
        {
            skjema.KontaktTelefon = null;
            skjema.KontaktEpost = null;
        }
        if (tilfelle == "ukjent landskode")
        {
            skjema.Landskode = "+999";
        }
        if (tilfelle == "bokstaver i telefon")
        {
            skjema.KontaktTelefon = "ring meg";
        }
        if (tilfelle == "annet uten tekst")
        {
            skjema.Type = BehovType.Annet;
        }

        Assert.Equal(forventet, ErGyldig(skjema));
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
    private static bool ErGyldig(BehovViewModel modell)
    {
        var resultater = new List<ValidationResult>();
        return Validator.TryValidateObject(modell, new ValidationContext(modell), resultater, validateAllProperties: true);
    }
}
