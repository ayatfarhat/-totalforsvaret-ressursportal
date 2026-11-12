using Nabohjelp.Models;
using Nabohjelp.Services;

namespace Nabohjelp.ViewModels;

// En liste med behov, delt i kategoriene Åpne, Pågår og Lukket (se BehovStatusflyt.Kategori),
// med filter på type, hast og kommune. Brukes av behovskøen (Behov/Ko) og «Mine behov».
//
// Slik henger det sammen:
//   1. Brukeren velger fane og filter. Valgene står i adressen, for eksempel
//      /Behov/Ko?kategori=pagar&prioritet=Urgent (kan lagres som bokmerke eller deles).
//   2. ASP.NET fyller egenskapene under (Kategori, Type ...) fra adressen.
//   3. BehovController henter behovene og kaller Fyll.
//   4. Siden viser Faner() og Liste.
public class BehovListeViewModel
{
    // Valgt kategori: "apne", "pagar", "lukket" eller "alle". Tom = standard for siden.
    public string? Kategori { get; set; }

    public BehovType? Type { get; set; }

    public BehovPrioritet? Prioritet { get; set; }

    // Kommunen slik brukeren skrev den i filteret («Grimstad»).
    public string? Kommune { get; set; }

    // "nyeste" (nyeste først) eller "akutte" (akutte først, deretter nyeste).
    public string? Sortering { get; set; }

    // Behovene som vises, etter filter og sortering.
    public List<Behov> Liste { get; set; } = new List<Behov>();

    // Hvor mange behov som passer filteret i hver kategori (tallene på fanene).
    public int AntallApne { get; set; }
    public int AntallPagar { get; set; }
    public int AntallLukket { get; set; }
    public int AntallAlle { get; set; }

    // Historikken til behovene i Liste. Fylles av BehovController (LeggTilHistorikk).
    public List<StatusEndring> Historikk { get; set; } = new List<StatusEndring>();

    // E-postadressen til dem som har endret status (brukerId → e-post).
    // Bare kriseledelsen får se hvem som gjorde endringen, så den er tom for frivillige.
    public Dictionary<string, string> Brukernavn { get; set; } = new Dictionary<string, string>();

    // Ressursene som er tildelt behovene nå (bare for kriseledelsen).
    public List<TildeltRessursViewModel> Tildelte { get; set; } = new List<TildeltRessursViewModel>();

    // Historikken til ett behov, klar for Views/Behov/_Historikk.cshtml.
    public HistorikkViewModel HistorikkFor(Behov behov)
    {
        var historikk = new HistorikkViewModel { Behov = behov, Brukernavn = Brukernavn };
        foreach (StatusEndring endring in Historikk)
        {
            if (endring.BehovId == behov.Id)
            {
                historikk.Endringer.Add(endring);
            }
        }
        return historikk;
    }

    // Ressursene som er tildelt ett behov nå.
    public List<TildeltRessursViewModel> TildelteFor(Behov behov)
    {
        var tildelte = new List<TildeltRessursViewModel>();
        foreach (TildeltRessursViewModel tildelt in Tildelte)
        {
            if (tildelt.BehovId == behov.Id)
            {
                tildelte.Add(tildelt);
            }
        }
        return tildelte;
    }

    // Sier om brukeren har valgt noe i filteret (da vises knappen «Nullstill»).
    public bool HarFilter()
    {
        return Type != null || Prioritet != null || !string.IsNullOrWhiteSpace(Kommune);
    }

    // Fanene over lista, i den rekkefølgen de vises.
    public List<Fane> Faner()
    {
        return new List<Fane>
        {
            LagFane(BehovStatusflyt.KategoriApne, "Åpne", AntallApne),
            LagFane(BehovStatusflyt.KategoriPagar, "Pågår", AntallPagar),
            LagFane(BehovStatusflyt.KategoriLukket, "Lukket", AntallLukket),
            LagFane(BehovStatusflyt.KategoriAlle, "Alle", AntallAlle)
        };
    }

    private Fane LagFane(string kategori, string norskTekst, int antall)
    {
        return new Fane
        {
            Kategori = kategori,
            Tekst = Oversetter.Tekst(norskTekst),
            Antall = antall,
            ErValgt = Kategori == kategori
        };
    }

    // Fyller lista fra alle behovene brukeren har lov til å se.
    // standardKategori brukes når ingen kategori er valgt (eller den er ukjent).
    public void Fyll(List<Behov> alle, string standardKategori)
    {
        BrukStandardvalg(standardKategori);

        // Kommunen i filteret som nummer (se Kommuner.NummerForFilter).
        string? kommunenummer = Kommuner.NummerForFilter(Kommune);

        Liste = new List<Behov>();
        AntallApne = 0;
        AntallPagar = 0;
        AntallLukket = 0;
        AntallAlle = 0;

        foreach (Behov behov in alle)
        {
            // Først filteret (type, hast og kommune) ...
            if (!PasserFilteret(behov, kommunenummer))
            {
                continue;
            }

            // ... så telles behovet i sin kategori, og bare den valgte kategorien vises.
            string kategori = BehovStatusflyt.Kategori(behov.Status);
            TellMed(kategori);

            if (Kategori == BehovStatusflyt.KategoriAlle || Kategori == kategori)
            {
                Liste.Add(behov);
            }
        }

        Liste.Sort(Sammenlign);
    }

    // Ukjente verdier i adressen byttes ut med standardvalgene.
    private void BrukStandardvalg(string standardKategori)
    {
        bool kjentKategori = Kategori == BehovStatusflyt.KategoriApne
            || Kategori == BehovStatusflyt.KategoriPagar
            || Kategori == BehovStatusflyt.KategoriLukket
            || Kategori == BehovStatusflyt.KategoriAlle;

        if (!kjentKategori)
        {
            Kategori = standardKategori;
        }
        if (Sortering != "akutte")
        {
            Sortering = "nyeste";
        }
    }

    // Sjekker ett behov mot filteret. Et tomt filterfelt (null) godtar alt.
    private bool PasserFilteret(Behov behov, string? kommunenummer)
    {
        if (Type != null && behov.Type != Type)
        {
            return false;
        }
        if (Prioritet != null && behov.Prioritet != Prioritet)
        {
            return false;
        }
        if (kommunenummer != null && behov.Kommunenummer != kommunenummer)
        {
            return false;
        }
        return true;
    }

    // Øker tallet på fanen for kategorien, og på fanen «Alle».
    private void TellMed(string kategori)
    {
        AntallAlle++;

        if (kategori == BehovStatusflyt.KategoriApne)
        {
            AntallApne++;
        }
        else if (kategori == BehovStatusflyt.KategoriPagar)
        {
            AntallPagar++;
        }
        else if (kategori == BehovStatusflyt.KategoriLukket)
        {
            AntallLukket++;
        }
    }

    // Rekkefølgen i lista. Gir et negativt tall hvis a skal stå før b.
    private int Sammenlign(Behov a, Behov b)
    {
        // «Akutte først»: Urgent (0) før Planned (1).
        if (Sortering == "akutte" && a.Prioritet != b.Prioritet)
        {
            return a.Prioritet.CompareTo(b.Prioritet);
        }
        // Ellers, og innenfor samme hast: nyeste først.
        return b.OpprettetTid.CompareTo(a.OpprettetTid);
    }
}
