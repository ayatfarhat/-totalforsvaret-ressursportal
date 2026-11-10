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
    public void Kommuner_Har357UnikeKommunerI15Fylker()
    {
        var nummer = new HashSet<string>();
        foreach (Kommune kommune in Kommuner.Alle)
        {
            nummer.Add(kommune.Nummer);
        }

        Assert.Equal(357, Kommuner.Alle.Count);
        Assert.Equal(357, nummer.Count);   // HashSet tar bare med hvert nummer én gang
        Assert.Equal(15, Kommuner.Fylker.Count);
    }

    [Fact]
    public void Finn_GirKommuneOgFylke_EllerNull()
    {
        Kommune? grimstad = Kommuner.Finn("4202");

        Assert.NotNull(grimstad);
        Assert.Equal("Grimstad", grimstad!.Navn);
        Assert.Equal("Agder", grimstad.Fylke);
        Assert.Null(Kommuner.Finn("9999"));
        Assert.Equal(Kommuner.IkkeOppgitt, Kommuner.VisNavn(null));
    }

    // =====================================
    // ----- Kommune på ressursskjemaet -----
    // =====================================
    [Theory]
    [InlineData("", true)]               // alt fylt ut
    [InlineData("KommuneNavn", false)]   // det viktigste er påkrevd:
    [InlineData("Telefon", false)]
    [InlineData("Navn", false)]
    [InlineData("Beskrivelse", false)]
    [InlineData("TilgjengeligFra", false)]
    public void Skjema_DetViktigsteErPakrevd(string tomtFelt, bool forventet)
    {
        RessursFormViewModel skjema = GyldigSkjema();
        if (tomtFelt == "KommuneNavn") skjema.KommuneNavn = null;
        if (tomtFelt == "Telefon") skjema.Telefon = null;
        if (tomtFelt == "Navn") skjema.Navn = null;
        if (tomtFelt == "Beskrivelse") skjema.Beskrivelse = null;
        if (tomtFelt == "TilgjengeligFra") skjema.TilgjengeligFra = null;

        Assert.Equal(forventet, ErGyldig(skjema));
        Assert.Equal("Lillesand", skjema.VisKommune());
    }

    [Theory]
    [InlineData("Grimstad", "4202")]
    [InlineData(" grimstad kommune ", "4202")]   // små bokstaver og «kommune» bak
    [InlineData("4215", "4215")]                 // nummeret går også
    [InlineData("Atlantis", null)]
    [InlineData("", null)]
    public void FinnFraTekst_ForstarNavnOgNummer(string tekst, string? nummer)
    {
        Kommune? kommune = Kommuner.FinnFraTekst(tekst);

        if (nummer == null)
        {
            Assert.Null(kommune);
        }
        else
        {
            Assert.Equal(nummer, kommune!.Nummer);
        }
    }

    // =====================================
    // ----- Ressursliste per kommune -----
    // =====================================
    [Fact]
    public void Utskrift_GruppererPerKommune()
    {
        RessursUtskriftViewModel utskrift = RessursUtskriftViewModel.Lag(TreRessurser(), null, null, DateTime.Now);

        // Grimstad (4202), Lillesand (4215) og til slutt ressursen uten kommune.
        Assert.Equal(3, utskrift.Grupper.Count);
        Assert.Equal("Grimstad", utskrift.Grupper[0].Kommune);
        Assert.Equal("Lillesand", utskrift.Grupper[1].Kommune);
        Assert.Equal(Kommuner.IkkeOppgitt, utskrift.Grupper[2].Kommune);
        Assert.Equal(3, utskrift.Antall);
    }

    [Fact]
    public void Utskrift_FiltrertPaKommune_ViserBareDen()
    {
        RessursUtskriftViewModel utskrift = RessursUtskriftViewModel.Lag(TreRessurser(), null, "4215", DateTime.Now);

        Assert.Single(utskrift.Grupper);
        Assert.Equal("Lillesand (Agder)", utskrift.VisOmrade());
    }

    [Fact]
    public void Utskrift_FiltrertPaFylke_TarIkkeMedUkjentKommune()
    {
        RessursUtskriftViewModel utskrift = RessursUtskriftViewModel.Lag(TreRessurser(), "Agder", null, DateTime.Now);

        // Bare Grimstad og Lillesand. Ressursen uten kommune er ikke med.
        Assert.Equal(2, utskrift.Grupper.Count);
        Assert.Equal(2, utskrift.Antall);
    }

    [Fact]
    public void Utskrift_UkjentFilter_ViserHeleLandet()
    {
        RessursUtskriftViewModel utskrift = RessursUtskriftViewModel.Lag(TreRessurser(), "Atlantis", "0000", DateTime.Now);

        Assert.Equal(3, utskrift.Antall);
        Assert.Equal("Hele landet", utskrift.VisOmrade());
    }

    private static List<RessursFormViewModel> TreRessurser()
    {
        return new List<RessursFormViewModel>
        {
            new RessursFormViewModel { Type = "Båt", Posisjon = "Havna, Lillesand", Kommunenummer = "4215" },
            new RessursFormViewModel { Type = "Aggregat", Posisjon = "Fjellveien 3, Grimstad", Kommunenummer = "4202" },
            new RessursFormViewModel { Type = "Traktor", Posisjon = "Gården", Kommunenummer = null }
        };
    }

    private static RessursFormViewModel GyldigSkjema()
    {
        return new RessursFormViewModel
        {
            Type = "Båt",
            Posisjon = "Havna, Lillesand",
            KommuneNavn = "Lillesand",
            Kommunenummer = "4215",
            TilgjengeligFra = DateTime.Now.AddHours(2),
            Beskrivelse = "Type og lengde: snekke, 18 fot",
            Navn = "Kari Nordmann",
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
