using Nabohjelp.Models;
using Xunit;

// TRENGER Å FIKSE PÅ KOMMENTARER I DENNE FILEN!!!

namespace Nabohjelp.Tests;

public class VisningstekstTests
{
    // ==================
    // ----- Status -----
    // ==================
    [Fact]
    public void Status_NyttBehov_GirNy()
    {
        // Kaller metoden som skal testes.
        var tekst = Visningstekst.Status(BehovStatus.New);

        // Sjekker at resultatet er som forventet.
        Assert.Equal("Ny", tekst);
    }

    [Fact]
    public void Status_UnderVurdering_GirUnderVurdering()
    {
        var tekst = Visningstekst.Status(BehovStatus.UnderReview);
        Assert.Equal("Under vurdering", tekst);
    }

    [Fact]
    public void Status_Tildelt_GirTildelt()
    {
        var tekst = Visningstekst.Status(BehovStatus.Assigned);
        Assert.Equal("Tildelt", tekst);
    }

    [Fact]
    public void Status_Lost_GirLost()
    {
        var tekst = Visningstekst.Status(BehovStatus.Resolved);
        Assert.Equal("Løst", tekst);
    }

    // ==================
    // ----- Behov -----
    // ==================

    [Fact]
    public void Type_Generator_GirAggregat()
    {
        var tekst = Visningstekst.Type(BehovType.Generator);
        Assert.Equal("Aggregat", tekst);
    }

    [Fact]
    public void Type_Annet_GirAnnet()
    {
        var tekst = Visningstekst.Type(BehovType.Annet);
        Assert.Equal("Annet", tekst);
    }

    // =====================
    // ----- Prioritet -----
    // =====================

    [Fact]
    public void Prioritet_Urgent_GirAkutt()
    {
        var tekst = Visningstekst.Prioritet(BehovPrioritet.Urgent);
        Assert.Equal("Akutt", tekst);
    }

    [Fact]
    public void Prioritet_Planned_GirPlanlagt()
    {
        var tekst = Visningstekst.Prioritet(BehovPrioritet.Planned);
        Assert.Equal("Planlagt", tekst);
    }
}