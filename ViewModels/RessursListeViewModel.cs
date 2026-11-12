using Nabohjelp.Models;
using Nabohjelp.Services;

namespace Nabohjelp.ViewModels;

// Ressurslisten for kriseledelsen (/Ressurs/Oversikt), delt i Tilgjengelige og Tildelte,
// med filter på type og kommune. Fungerer som behovskøen (se BehovListeViewModel):
// valgene står i adressen, RessursController kaller Fyll, og siden viser Faner() og Liste.
public class RessursListeViewModel
{
    public const string KategoriTilgjengelig = "tilgjengelig";
    public const string KategoriTildelt = "tildelt";
    public const string KategoriAlle = "alle";

    // Valgt kategori. Tom eller ukjent = «tilgjengelig».
    public string? Kategori { get; set; }

    // En av ressurstypene (RessursFormViewModel.Typer), eller tom for alle.
    public string? Type { get; set; }

    // Kommunen slik brukeren skrev den i filteret («Grimstad»).
    public string? Kommune { get; set; }

    // Ressursene som vises, nyeste først.
    public List<RessursFormViewModel> Liste { get; set; } = new List<RessursFormViewModel>();

    // Hvor mange ressurser som passer filteret i hver kategori (tallene på fanene).
    public int AntallTilgjengelig { get; set; }
    public int AntallTildelt { get; set; }
    public int AntallAlle { get; set; }

    // Sier om brukeren har valgt noe i filteret (da vises knappen «Nullstill»).
    public bool HarFilter()
    {
        return !string.IsNullOrWhiteSpace(Type) || !string.IsNullOrWhiteSpace(Kommune);
    }

    // Fanene over lista, i den rekkefølgen de vises.
    public List<Fane> Faner()
    {
        return new List<Fane>
        {
            LagFane(KategoriTilgjengelig, "Tilgjengelige", AntallTilgjengelig),
            LagFane(KategoriTildelt, "Tildelte", AntallTildelt),
            LagFane(KategoriAlle, "Alle", AntallAlle)
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

    // Fyller lista. alle må være sortert slik den skal vises (controlleren gir nyeste først).
    public void Fyll(List<RessursFormViewModel> alle)
    {
        // Ukjent kategori i adressen byttes ut med «tilgjengelig».
        if (Kategori != KategoriTildelt && Kategori != KategoriAlle)
        {
            Kategori = KategoriTilgjengelig;
        }

        // Kommunen i filteret som nummer (se Kommuner.NummerForFilter).
        string? kommunenummer = Kommuner.NummerForFilter(Kommune);

        Liste = new List<RessursFormViewModel>();
        AntallTilgjengelig = 0;
        AntallTildelt = 0;
        AntallAlle = 0;

        foreach (RessursFormViewModel ressurs in alle)
        {
            if (!PasserFilteret(ressurs, kommunenummer))
            {
                continue;
            }

            // «Tilgjengelig» er ledig. Alt annet (i dag bare «Tildelt») er i bruk.
            string kategori = KategoriTildelt;
            if (ressurs.Status == "Tilgjengelig")
            {
                kategori = KategoriTilgjengelig;
                AntallTilgjengelig++;
            }
            else
            {
                AntallTildelt++;
            }
            AntallAlle++;

            if (Kategori == KategoriAlle || Kategori == kategori)
            {
                Liste.Add(ressurs);
            }
        }
    }

    // Sjekker én ressurs mot filteret. Et tomt filterfelt godtar alt.
    private bool PasserFilteret(RessursFormViewModel ressurs, string? kommunenummer)
    {
        if (!string.IsNullOrWhiteSpace(Type) && ressurs.Type != Type)
        {
            return false;
        }
        if (kommunenummer != null && ressurs.Kommunenummer != kommunenummer)
        {
            return false;
        }
        return true;
    }
}
