using Nabohjelp.Models;

namespace Nabohjelp.ViewModels;

// En liste med behov, delt i kategoriene Åpne, Pågår og Lukket (se BehovStatusflyt.Kategori),
// med filter på type, hast og kommune. Brukes av behovskøen (Behov/Ko) og «Mine behov».
// Filteret kommer fra adressen, for eksempel /Behov/Ko?kategori=pagar&prioritet=Urgent,
// så en filtrert liste kan lagres som bokmerke eller deles.
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

    public bool HarFilter()
    {
        return Type != null || Prioritet != null || !string.IsNullOrWhiteSpace(Kommune);
    }

    // Fyller lista fra alle behovene brukeren har lov til å se.
    // standardKategori brukes når ingen kategori er valgt (eller den er ukjent).
    public void Fyll(List<Behov> alle, string standardKategori)
    {
        if (Kategori != BehovStatusflyt.KategoriApne && Kategori != BehovStatusflyt.KategoriPagar
            && Kategori != BehovStatusflyt.KategoriLukket && Kategori != BehovStatusflyt.KategoriAlle)
        {
            Kategori = standardKategori;
        }
        if (Sortering != "akutte")
        {
            Sortering = "nyeste";
        }

        // Kommunen i filteret gjøres om til nummer. En ukjent kommune gir ingen treff.
        string? kommunenummer = null;
        if (!string.IsNullOrWhiteSpace(Kommune))
        {
            // (Fullt navn på typen, fordi egenskapen over også heter Kommune.)
            Nabohjelp.Models.Kommune? valgt = Kommuner.FinnFraTekst(Kommune);
            if (valgt == null)
            {
                kommunenummer = "ukjent";
            }
            else
            {
                kommunenummer = valgt.Nummer;
            }
        }

        Liste = new List<Behov>();
        AntallApne = 0;
        AntallPagar = 0;
        AntallLukket = 0;
        AntallAlle = 0;

        foreach (Behov behov in alle)
        {
            // Først filteret (type, hast og kommune) ...
            bool passer = (Type == null || behov.Type == Type)
                && (Prioritet == null || behov.Prioritet == Prioritet)
                && (kommunenummer == null || behov.Kommunenummer == kommunenummer);
            if (!passer)
            {
                continue;
            }

            // ... så telles det per kategori, og bare den valgte kategorien vises.
            string kategori = BehovStatusflyt.Kategori(behov.Status);
            AntallAlle++;
            if (kategori == BehovStatusflyt.KategoriApne) AntallApne++;
            if (kategori == BehovStatusflyt.KategoriPagar) AntallPagar++;
            if (kategori == BehovStatusflyt.KategoriLukket) AntallLukket++;

            if (Kategori == BehovStatusflyt.KategoriAlle || Kategori == kategori)
            {
                Liste.Add(behov);
            }
        }

        Liste.Sort(Sammenlign);
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
