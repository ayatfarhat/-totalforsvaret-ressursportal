using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using Xunit;

namespace Nabohjelp.Tests;

// Tester historikken til behovene, reglene for å angre en tildeling og
// «Tilgjengelig fra» i ressursskjemaet. Ingen database trengs.
public class Statushistorikk_Tests
{
    [Theory]
    [InlineData(BehovStatus.New, false)]
    [InlineData(BehovStatus.UnderReview, false)]
    [InlineData(BehovStatus.Assigned, true)]
    [InlineData(BehovStatus.Resolved, false)]
    [InlineData(BehovStatus.Rejected, false)]
    [InlineData(BehovStatus.Withdrawn, false)]
    public void TildelingKanBareAngresNarBehovetErTildelt(BehovStatus status, bool kanAngres)
    {
        Assert.Equal(kanAngres, BehovStatusflyt.KanAngreTildeling(status));
    }

    [Fact]
    public void AngretTildeling_GirUnderVurdering_NarDetIkkeErFlereRessurser()
    {
        Assert.Equal(BehovStatus.UnderReview, BehovStatusflyt.StatusEtterAngretTildeling(0));
        Assert.Equal(BehovStatus.Assigned, BehovStatusflyt.StatusEtterAngretTildeling(1));
    }

    [Fact]
    public void Hendelse_ViserStatusTildelingOgAngring()
    {
        var aggregat = new Ressurs { Type = "Aggregat" };
        var pumpe = new Ressurs { Type = "Annet", AnnetType = "Vannpumpe" };

        StatusEndring vurdering = StatusEndring.Ny(1, BehovStatus.New, BehovStatus.UnderReview, "kommune");
        StatusEndring tildelt = StatusEndring.ForTildeling(1, BehovStatus.UnderReview, BehovStatus.Assigned, "kommune", aggregat, false);
        StatusEndring angret = StatusEndring.ForTildeling(1, BehovStatus.Assigned, BehovStatus.UnderReview, "kommune", pumpe, true);

        Assert.Equal("Under vurdering", Visningstekst.Hendelse(vurdering));
        Assert.Equal("Tildelt: Aggregat", Visningstekst.Hendelse(tildelt));
        Assert.Equal("Tildelingen av Annet: Vannpumpe ble angret", Visningstekst.Hendelse(angret));
    }

    [Fact]
    public void Historikk_ViserHvemBareNarNavneneErMed()
    {
        StatusEndring kjent = StatusEndring.Ny(1, BehovStatus.New, BehovStatus.UnderReview, "id-1");
        StatusEndring slettet = StatusEndring.Ny(1, BehovStatus.UnderReview, BehovStatus.Resolved, null);

        // Frivillige får ingen navn: da vises ikke hvem.
        var forFrivillig = new HistorikkViewModel();
        Assert.Null(forFrivillig.Hvem(kjent));

        // Kriseledelsen ser e-posten, og «slettet konto» hvis kontoen er borte.
        var forKommunen = new HistorikkViewModel();
        forKommunen.Brukernavn["id-1"] = "kommune@test.no";
        Assert.Equal("kommune@test.no", forKommunen.Hvem(kjent));
        Assert.Equal("slettet konto", forKommunen.Hvem(slettet));
    }

    [Fact]
    public void HistorikkFor_TarBareMedEndringerForBehovet()
    {
        var behov1 = new Behov { Id = 1 };
        var behov2 = new Behov { Id = 2 };
        var liste = new BehovListeViewModel();
        liste.Historikk.Add(StatusEndring.Ny(1, BehovStatus.New, BehovStatus.UnderReview, "a"));
        liste.Historikk.Add(StatusEndring.Ny(2, BehovStatus.New, BehovStatus.Rejected, "a"));
        liste.Historikk.Add(StatusEndring.Ny(1, BehovStatus.UnderReview, BehovStatus.Resolved, "a"));

        HistorikkViewModel historikk = liste.HistorikkFor(behov1);

        Assert.Equal(2, historikk.Endringer.Count);
        Assert.Equal(BehovStatus.Resolved, historikk.Endringer[1].TilStatus);
        Assert.Single(liste.HistorikkFor(behov2).Endringer);
    }

    [Fact]
    public void TilgjengeligFra_LittTilbakeITidGodtas_MenIkkeLengeSiden()
    {
        // Skjemaet fyller inn tiden nå, og det tar tid å fylle ut resten.
        var nettopp = new RessursFormViewModel { TilgjengeligFra = DateTime.Now.AddMinutes(-20) };
        var lengeSiden = new RessursFormViewModel { TilgjengeligFra = DateTime.Now.AddHours(-3) };
        var tomt = new RessursFormViewModel { TilgjengeligFra = null };

        Assert.True(nettopp.ErValidTilgjengeligFra());
        Assert.False(lengeSiden.ErValidTilgjengeligFra());
        Assert.True(tomt.ErValidTilgjengeligFra());
    }
}
