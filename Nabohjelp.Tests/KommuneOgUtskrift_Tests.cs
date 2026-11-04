using System.ComponentModel.DataAnnotations;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for kommunelisten (Kommuner), kommunefeltet på ressursskjemaet
// og ressurslisten per kommune (RessursUtskriftViewModel).
public class KommuneOgUtskriftTests
{
    // ===========================
    // ----- Kommunelisten -----
    // ===========================
    [Fact]
    public void Kommuner_HarAlle357()
    {
        Assert.Equal(357, Kommuner.Alle.Count);
    }

    [Fact]
    public void Kommuner_HarUnikeNummer()
    {
        Assert.Equal(Kommuner.Alle.Count, Kommuner.Alle.Select(k => k.Nummer).Distinct().Count());
    }

    [Fact]
    public void Kommuner_HarFemtenFylker()
    {
        Assert.Equal(15, Kommuner.Fylker.Count());
    }

    [Fact]
    public void Finn_GirKommuneOgFylke()
    {
        var grimstad = Kommuner.Finn("4202");

        Assert.NotNull(grimstad);
        Assert.Equal("Grimstad", grimstad!.Navn);
        Assert.Equal("Agder", grimstad.Fylke);
    }

    [Fact]
    public void Finn_UkjentNummer_GirNull()
    {
        Assert.Null(Kommuner.Finn("9999"));
        Assert.False(Kommuner.Finnes(""));
    }

    [Fact]
    public void VisNavn_UtenKommune_GirIkkeOppgitt()
    {
        Assert.Equal(Kommuner.IkkeOppgitt, Kommuner.VisNavn(null));
    }

    // ===================================
    // ----- Kommune på ressursskjemaet -----
    // ===================================
    [Fact]
    public void Skjema_UtenKommune_ErUgyldig()
    {
        var skjema = GyldigSkjema();
        skjema.Kommunenummer = null;

        Assert.False(ErGyldig(skjema));
    }

    [Fact]
    public void Skjema_MedKommune_ErGyldig()
    {
        Assert.True(ErGyldig(GyldigSkjema()));
    }

    [Fact]
    public void VisKommune_ViserNavnet()
    {
        Assert.Equal("Lillesand", GyldigSkjema().VisKommune());
    }

    // =====================================
    // ----- Ressursliste per kommune -----
    // =====================================
    [Fact]
    public void Utskrift_GruppererPerKommune()
    {
        var utskrift = RessursUtskriftViewModel.Lag(TreRessurser(), null, null, DateTime.Now);

        // Grimstad (4202), Lillesand (4215) og til slutt ressursen uten kommune.
        Assert.Equal(new[] { "Grimstad", "Lillesand", Kommuner.IkkeOppgitt }, utskrift.Grupper.Select(g => g.Kommune));
        Assert.Equal(3, utskrift.Antall);
    }

    [Fact]
    public void Utskrift_FiltrertPaKommune_ViserBareDen()
    {
        var utskrift = RessursUtskriftViewModel.Lag(TreRessurser(), null, "4215", DateTime.Now);

        Assert.Single(utskrift.Grupper);
        Assert.Equal("Lillesand (Agder)", utskrift.VisOmrade());
    }

    [Fact]
    public void Utskrift_FiltrertPaFylke_TarIkkeMedUkjentKommune()
    {
        var utskrift = RessursUtskriftViewModel.Lag(TreRessurser(), "Agder", null, DateTime.Now);

        Assert.Equal(2, utskrift.Antall);
        Assert.DoesNotContain(utskrift.Grupper, g => g.Kommune == Kommuner.IkkeOppgitt);
    }

    [Fact]
    public void Utskrift_UkjentFilter_ViserHeleLandet()
    {
        var utskrift = RessursUtskriftViewModel.Lag(TreRessurser(), "Atlantis", "0000", DateTime.Now);

        Assert.Equal(3, utskrift.Antall);
        Assert.Equal("Hele landet", utskrift.VisOmrade());
    }

    private static List<RessursFormViewModel> TreRessurser()
    {
        return new List<RessursFormViewModel>
        {
            new() { Type = "Båt", Posisjon = "Havna, Lillesand", Kommunenummer = "4215" },
            new() { Type = "Aggregat", Posisjon = "Fjellveien 3, Grimstad", Kommunenummer = "4202" },
            new() { Type = "Traktor", Posisjon = "Gården", Kommunenummer = null }
        };
    }

    private static RessursFormViewModel GyldigSkjema()
    {
        return new RessursFormViewModel
        {
            Type = "Båt",
            Posisjon = "Havna, Lillesand",
            Kommunenummer = "4215",
            Landskode = "+47",
            Telefon = "41234567"
        };
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
