using System.ComponentModel.DataAnnotations;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for telefonnummer med landskode (RessursFormViewModel)
// og for ønskede ressurser (OnsketRessurs).
public class RessursOgOnsketTests
{
    // ===============================================
    // ----- Telefon med landskode (lagres som «+47 12345678») -----
    // ===============================================
    [Theory]
    [InlineData("+358 40 1234567", "+358 40 1234567", "+358401234567")]   // landskode fra lista
    [InlineData("41234567", "+47 41234567", "+4741234567")]               // gammelt nummer: Norge
    [InlineData("+4741234567", "+4741234567", "+4741234567")]             // ukjent format: vises som lagret
    [InlineData(null, "Ikke oppgitt", "")]                                // ingen telefon
    public void LesTelefonFraLagring_ViserNummeret(string? lagret, string vist, string lenke)
    {
        var skjema = new RessursFormViewModel();

        skjema.LesTelefonFraLagring(lagret);

        Assert.Equal(vist, skjema.VisTelefon());
        Assert.Equal(lenke, skjema.TelefonLenke());
    }

    [Fact]
    public void Telefon_LagretOgLestIgjen_BlirLikt()
    {
        var forSkjema = new RessursFormViewModel { Landskode = "+44", Telefon = "7700 900123" };
        Assert.Equal("+44 7700 900123", forSkjema.TelefonForLagring());

        var etterSkjema = new RessursFormViewModel();
        etterSkjema.LesTelefonFraLagring(forSkjema.TelefonForLagring());
        Assert.Equal("+44", etterSkjema.Landskode);
        Assert.Equal("7700 900123", etterSkjema.Telefon);

        // Tomt nummer lagres som ingenting.
        Assert.Null(new RessursFormViewModel { Telefon = "  " }.TelefonForLagring());
    }

    // =============================
    // ----- Ønskede ressurser -----
    // =============================
    [Theory]
    [InlineData(2, "Lillesand", true)]
    [InlineData(0, "Lillesand", false)]   // antall må være minst 1
    [InlineData(1, "", false)]            // stedet må fylles ut
    public void OnsketRessurs_Validering(int antall, string sted, bool forventet)
    {
        var onsket = new OnsketRessurs { Type = "Båt", Antall = antall, Sted = sted };

        Assert.Equal(forventet, ErGyldig(onsket));
        Assert.Equal("Ingen", onsket.VisBeskrivelse());
    }

    // =====================================
    // ----- «Annet» med egen tekst -----
    // =====================================
    [Fact]
    public void KopierTil_LagrerAnnetTekstBareNaarTypenErAnnet()
    {
        var ressurs = new Ressurs();
        var skjema = new RessursFormViewModel { Type = "Annet", AnnetType = " vannpumpe ", Posisjon = "Grimstad" };

        skjema.KopierTil(ressurs);
        Assert.Equal("vannpumpe", ressurs.AnnetType);
        Assert.Equal("Annet: vannpumpe", RessursFormViewModel.FraRessurs(ressurs, null).VisType());

        skjema.Type = "Båt";
        skjema.KopierTil(ressurs);
        Assert.Null(ressurs.AnnetType);
        Assert.Equal("Båt", RessursFormViewModel.FraRessurs(ressurs, null).VisType());
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
