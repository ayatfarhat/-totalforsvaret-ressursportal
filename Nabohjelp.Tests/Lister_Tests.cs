using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for fanene og filteret i behovskøen, «Mine behov» (BehovListeViewModel)
// og ressurslisten (RessursListeViewModel).
public class ListerTests
{
    private static readonly DateTime Naa = new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(BehovStatus.New, "apne")]
    [InlineData(BehovStatus.UnderReview, "pagar")]
    [InlineData(BehovStatus.Assigned, "pagar")]
    [InlineData(BehovStatus.Resolved, "lukket")]
    [InlineData(BehovStatus.Rejected, "lukket")]
    [InlineData(BehovStatus.Withdrawn, "lukket")]
    public void Kategori_DelerStatuseneIApnePagarOgLukket(BehovStatus status, string kategori)
    {
        Assert.Equal(kategori, BehovStatusflyt.Kategori(status));
    }

    // Fire behov: to åpne (det nyeste er planlagt), ett som pågår og ett lukket.
    private static List<Behov> Behovene()
    {
        return new List<Behov>
        {
            new Behov { Id = 1, Status = BehovStatus.New, Prioritet = BehovPrioritet.Urgent, Type = BehovType.Generator, Kommunenummer = "4202", OpprettetTid = Naa.AddHours(-3) },
            new Behov { Id = 2, Status = BehovStatus.New, Prioritet = BehovPrioritet.Planned, Type = BehovType.Transport, Kommunenummer = "4215", OpprettetTid = Naa.AddHours(-1) },
            new Behov { Id = 3, Status = BehovStatus.Assigned, Prioritet = BehovPrioritet.Urgent, Type = BehovType.Generator, OpprettetTid = Naa.AddHours(-2) },
            new Behov { Id = 4, Status = BehovStatus.Resolved, Prioritet = BehovPrioritet.Planned, Type = BehovType.Drone, OpprettetTid = Naa.AddHours(-5) }
        };
    }

    [Fact]
    public void Behovsliste_ViserValgtKategoriMedNyesteForst_OgTellerAlleKategorier()
    {
        var liste = new BehovListeViewModel();

        liste.Fyll(Behovene(), BehovStatusflyt.KategoriApne);

        // Ingen kategori valgt: standarden (åpne) brukes, med det nyeste behovet først.
        Assert.Equal(new List<int> { 2, 1 }, Ider(liste.Liste));
        Assert.Equal(2, liste.AntallApne);
        Assert.Equal(1, liste.AntallPagar);
        Assert.Equal(1, liste.AntallLukket);
        Assert.Equal(4, liste.AntallAlle);
    }

    [Fact]
    public void Behovsliste_FilterOgSortering()
    {
        // «Akutte først» på fanen Alle: akutte (nyeste først), deretter planlagte.
        var akutte = new BehovListeViewModel { Kategori = "alle", Sortering = "akutte" };
        akutte.Fyll(Behovene(), BehovStatusflyt.KategoriApne);
        Assert.Equal(new List<int> { 3, 1, 2, 4 }, Ider(akutte.Liste));

        // Filter på kommune (skrevet med små bokstaver) og type.
        var grimstad = new BehovListeViewModel { Kategori = "alle", Kommune = "grimstad", Type = BehovType.Generator };
        grimstad.Fyll(Behovene(), BehovStatusflyt.KategoriApne);
        Assert.Equal(new List<int> { 1 }, Ider(grimstad.Liste));
        Assert.True(grimstad.HarFilter());

        // En kommune som ikke finnes, gir ingen treff (ikke alle).
        var ukjent = new BehovListeViewModel { Kategori = "alle", Kommune = "Atlantis" };
        ukjent.Fyll(Behovene(), BehovStatusflyt.KategoriApne);
        Assert.Empty(ukjent.Liste);
    }

    [Fact]
    public void Behovskø_BeholderPosisjonOgKommuneForKartlenke()
    {
        var behov = new Behov
        {
            Id = 42,
            Status = BehovStatus.New,
            Type = BehovType.Transport,
            Kommunenummer = "4204",
            Latitude = 58.1467,
            Longitude = 7.9956
        };

        var liste = new BehovListeViewModel { Kategori = "alle" };
        liste.Fyll(new List<Behov> { behov }, BehovStatusflyt.KategoriApne);

        Behov vist = Assert.Single(liste.Liste);
        Assert.Equal("4204", vist.Kommunenummer);
        Assert.Equal(58.1467, vist.Latitude);
        Assert.Equal(7.9956, vist.Longitude);
    }

    [Fact]
    public void Ressursliste_ViserTilgjengeligeForst_OgFiltrererPaType()
    {
        var ressurser = new List<RessursFormViewModel>
        {
            new RessursFormViewModel { Id = 1, Type = "Båt", Status = "Tilgjengelig", Kommunenummer = "4215" },
            new RessursFormViewModel { Id = 2, Type = "Aggregat", Status = "Tildelt", Kommunenummer = "4202" },
            new RessursFormViewModel { Id = 3, Type = "Aggregat", Status = "Tilgjengelig", Kommunenummer = "4202" }
        };

        var liste = new RessursListeViewModel();
        liste.Fyll(ressurser);
        Assert.Equal(new List<int> { 1, 3 }, RessursIder(liste.Liste));
        Assert.Equal(1, liste.AntallTildelt);

        var aggregater = new RessursListeViewModel { Kategori = "alle", Type = "Aggregat" };
        aggregater.Fyll(ressurser);
        Assert.Equal(new List<int> { 2, 3 }, RessursIder(aggregater.Liste));
        Assert.Equal(2, aggregater.AntallAlle);
    }

    [Fact]
    public void Faner_ViserAntallOgHvilkenSomErValgt()
    {
        var liste = new BehovListeViewModel { Kategori = "pagar" };
        liste.Fyll(Behovene(), BehovStatusflyt.KategoriApne);

        List<Fane> faner = liste.Faner();

        Assert.Equal(4, faner.Count);
        Assert.Equal("Åpne", faner[0].Tekst);
        Assert.Equal(2, faner[0].Antall);
        Assert.False(faner[0].ErValgt);
        Assert.Equal("pagar", faner[1].Kategori);
        Assert.True(faner[1].ErValgt);
    }

    // Id-ene til behovene i lista, i samme rekkefølge. Gjør det lett å sjekke rekkefølgen.
    private static List<int> Ider(List<Behov> behovsliste)
    {
        var ider = new List<int>();
        foreach (Behov behov in behovsliste)
        {
            ider.Add(behov.Id);
        }
        return ider;
    }

    private static List<int> RessursIder(List<RessursFormViewModel> ressurser)
    {
        var ider = new List<int>();
        foreach (RessursFormViewModel ressurs in ressurser)
        {
            ider.Add(ressurs.Id);
        }
        return ider;
    }
}
