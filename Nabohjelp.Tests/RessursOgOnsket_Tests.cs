using System.ComponentModel.DataAnnotations;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for telefonnummer med landskode (RessursFormViewModel)
// og for ønskede ressurser (OnsketRessurs).
public class RessursOgOnsketTests
{
    // =========================================
    // ----- Telefon: lagring i databasen -----
    // =========================================
    [Fact]
    public void TelefonForLagring_MedNummer_LagrerLandskodeOgNummer()
    {
        var skjema = new RessursFormViewModel { Landskode = "+46", Telefon = "70 123 45 67" };

        Assert.Equal("+46 70 123 45 67", skjema.TelefonForLagring());
    }

    [Fact]
    public void TelefonForLagring_UtenNummer_LagrerIngenting()
    {
        var skjema = new RessursFormViewModel { Landskode = "+47", Telefon = "  " };

        Assert.Null(skjema.TelefonForLagring());
    }

    [Fact]
    public void LesTelefonFraLagring_DelerOppLandskodeOgNummer()
    {
        var skjema = new RessursFormViewModel();

        skjema.LesTelefonFraLagring("+358 40 1234567");

        Assert.Equal("+358", skjema.Landskode);
        Assert.Equal("40 1234567", skjema.Telefon);
        Assert.Equal("+358 40 1234567", skjema.VisTelefon());
        Assert.Equal("+358401234567", skjema.TelefonLenke());
    }

    [Fact]
    public void LesTelefonFraLagring_GammeltNummerUtenLandskode_BrukerNorge()
    {
        var skjema = new RessursFormViewModel();

        skjema.LesTelefonFraLagring("41234567");

        Assert.Equal("+47", skjema.Landskode);
        Assert.Equal("+47 41234567", skjema.VisTelefon());
    }

    [Fact]
    public void LesTelefonFraLagring_UkjentLandskode_VisesSomDenErLagret()
    {
        var skjema = new RessursFormViewModel();

        skjema.LesTelefonFraLagring("+4741234567");

        Assert.Equal("+4741234567", skjema.VisTelefon());
    }

    [Fact]
    public void LesTelefonFraLagring_Tom_GirIkkeOppgitt()
    {
        var skjema = new RessursFormViewModel();

        skjema.LesTelefonFraLagring(null);

        Assert.Equal("Ikke oppgitt", skjema.VisTelefon());
        Assert.Equal("", skjema.TelefonLenke());
    }

    [Fact]
    public void Telefon_LagretOgLestIgjen_BlirLikt()
    {
        var forSkjema = new RessursFormViewModel { Landskode = "+44", Telefon = "7700 900123" };

        var etterSkjema = new RessursFormViewModel();
        etterSkjema.LesTelefonFraLagring(forSkjema.TelefonForLagring());

        Assert.Equal(forSkjema.VisTelefon(), etterSkjema.VisTelefon());
    }

    // =============================
    // ----- Ønskede ressurser -----
    // =============================
    [Fact]
    public void OnsketRessurs_UtenBeskrivelse_ViserIngen()
    {
        var onsket = new OnsketRessurs { Type = "Aggregat", Sted = "Grimstad" };

        Assert.Equal("Ingen", onsket.VisBeskrivelse());
    }

    [Fact]
    public void OnsketRessurs_GyldigOnske_ErGyldig()
    {
        var onsket = new OnsketRessurs { Type = "Båt", Antall = 2, Sted = "Lillesand" };

        Assert.True(ErGyldig(onsket));
    }

    [Fact]
    public void OnsketRessurs_AntallNull_ErUgyldig()
    {
        var onsket = new OnsketRessurs { Type = "Båt", Antall = 0, Sted = "Lillesand" };

        Assert.False(ErGyldig(onsket));
    }

    [Fact]
    public void OnsketRessurs_UtenSted_ErUgyldig()
    {
        var onsket = new OnsketRessurs { Type = "Båt", Antall = 1, Sted = "" };

        Assert.False(ErGyldig(onsket));
    }

    // Kjører de samme valideringsreglene som ASP.NET bruker på skjemaet.
    private static bool ErGyldig(object modell)
    {
        var resultater = new List<ValidationResult>();
        return Validator.TryValidateObject(
            modell,
            new ValidationContext(modell),
            resultater,
            validateAllProperties: true);
    }
}
