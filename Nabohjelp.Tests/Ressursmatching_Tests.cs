using Nabohjelp.Models;
using Nabohjelp.Services;
using Nabohjelp.ViewModels;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for hvilke ressurser som passer til et behov (Services/Ressursmatching.cs)
// og for boksene på Kommandobro-kartet (ViewModels/MapViewModel.cs).
public class RessursmatchingTests
{
    // Et behov for strøm i Grimstad sentrum.
    private static Behov StromIGrimstad()
    {
        return new Behov { Type = BehovType.Generator, Latitude = 58.3405, Longitude = 8.5934 };
    }

    private static Ressurs Aggregat(int id, decimal? bredde, decimal? lengde, string status = "Tilgjengelig")
    {
        return new Ressurs { Id = id, Type = "Aggregat", Status = status, Breddegrad = bredde, Lengdegrad = lengde };
    }

    // ==========================
    // ----- Passer typen? -----
    // ==========================
    [Theory]
    [InlineData(BehovType.Generator, "Aggregat", true)]
    [InlineData(BehovType.Generator, "Båt", false)]
    [InlineData(BehovType.Drone, "Drone og droneoperatør", true)]
    [InlineData(BehovType.Transport, "Traktor", true)]
    [InlineData(BehovType.Transport, "Lokaler", false)]
    [InlineData(BehovType.Evakuering, "Lokaler", true)]
    [InlineData(BehovType.Annet, "Sand/grus", true)]
    public void PasserTilBehov_FolgerReglene(BehovType behovType, string ressursType, bool forventet)
    {
        Assert.Equal(forventet, Ressursmatching.PasserTilBehov(behovType, ressursType));
    }

    // ===================================
    // ----- Nærmeste ledige ressurs -----
    // ===================================
    [Fact]
    public void FinnNaermeste_VelgerDenSomErNaermest()
    {
        var ressurser = new List<Ressurs>
        {
            Aggregat(1, 58.1467m, 7.9956m),   // Kristiansand, omtrent 40 km unna
            Aggregat(2, 58.3500m, 8.6000m)    // Grimstad, omtrent 1 km unna
        };

        var naermeste = Ressursmatching.FinnNaermeste(StromIGrimstad(), ressurser);

        Assert.NotNull(naermeste);
        Assert.Equal(2, naermeste!.Ressurs.Id);
        Assert.InRange(naermeste.AvstandKm, 0.5, 2.0);
    }

    [Fact]
    public void FinnNaermeste_HopperOverTildelteFeilTypeOgUtenPosisjon()
    {
        var ressurser = new List<Ressurs>
        {
            Aggregat(1, 58.3500m, 8.6000m, status: "Tildelt"),
            new Ressurs { Id = 2, Type = "Båt", Status = "Tilgjengelig", Breddegrad = 58.3410m, Lengdegrad = 8.5940m },
            Aggregat(3, null, null),
            Aggregat(4, 58.1467m, 7.9956m)
        };

        var naermeste = Ressursmatching.FinnNaermeste(StromIGrimstad(), ressurser);

        Assert.Equal(4, naermeste!.Ressurs.Id);
    }

    [Fact]
    public void FinnNaermeste_IngenSomPasser_GirNull()
    {
        var ressurser = new List<Ressurs> { Aggregat(1, 58.35m, 8.60m, status: "Tildelt") };

        Assert.Null(Ressursmatching.FinnNaermeste(StromIGrimstad(), ressurser));
    }

    // ==============================
    // ----- Boksen på kartet -----
    // ==============================
    [Fact]
    public void LeggTilLinje_TommeVerdierVisesIkke()
    {
        var punkt = new KartPunkt();

        punkt.LeggTilLinje("Telefon", "41234567");
        punkt.LeggTilLinje("E-post", null);
        punkt.LeggTilLinje("Navn", "   ");

        Assert.Single(punkt.Linjer);
        Assert.Equal("Telefon", punkt.Linjer[0].Etikett);
    }
}
