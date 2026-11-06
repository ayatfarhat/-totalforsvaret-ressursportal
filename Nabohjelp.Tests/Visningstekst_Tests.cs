using Nabohjelp.Models;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for teksten og fargene som vises for behov (Models/Visningstekst.cs).
// Enhetstestene har ikke oversettelsene, så tekstene kommer på norsk.
public class VisningstekstTests
{
    [Theory]
    [InlineData(BehovStatus.New, "Ny", "")]
    [InlineData(BehovStatus.UnderReview, "Under vurdering", "merke-lys")]
    [InlineData(BehovStatus.Assigned, "Tildelt", "merke-gi")]
    [InlineData(BehovStatus.Resolved, "Løst", "merke-gi")]
    [InlineData(BehovStatus.Rejected, "Avvist", "merke-hjelp")]
    [InlineData(BehovStatus.Withdrawn, "Trukket tilbake", "")]
    public void Status_GirTekstOgFarge(BehovStatus status, string tekst, string farge)
    {
        Assert.Equal(tekst, Visningstekst.Status(status));
        Assert.Equal(farge, Visningstekst.StatusFarge(status));
    }

    [Theory]
    [InlineData(BehovType.Generator, null, "Aggregat")]
    [InlineData(BehovType.Annet, null, "Annet")]
    [InlineData(BehovType.Annet, " vannlekkasje ", "Annet: vannlekkasje")]   // det brukeren skrev
    [InlineData(BehovType.Transport, "ignoreres", "Transport")]              // teksten gjelder bare «Annet»
    public void Type_GirTekst(BehovType type, string? annetType, string forventet)
    {
        Assert.Equal(forventet, Visningstekst.Type(type, annetType));
    }

    [Fact]
    public void Prioritet_GirAkuttEllerPlanlagt()
    {
        Assert.Equal("Akutt", Visningstekst.Prioritet(BehovPrioritet.Urgent));
        Assert.Equal("Planlagt", Visningstekst.Prioritet(BehovPrioritet.Planned));
    }
}
