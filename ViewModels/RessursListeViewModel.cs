using Nabohjelp.Models;

namespace Nabohjelp.ViewModels;

// Ressurslisten for kriseledelsen (/Ressurs/Oversikt), delt i Tilgjengelige og Tildelte,
// med filter på type og kommune. Fungerer som behovskøen (se BehovListeViewModel).
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

    public int AntallTilgjengelig { get; set; }
    public int AntallTildelt { get; set; }
    public int AntallAlle { get; set; }

    public bool HarFilter()
    {
        return !string.IsNullOrWhiteSpace(Type) || !string.IsNullOrWhiteSpace(Kommune);
    }

    // Fyller lista. alle må være sortert slik den skal vises (controlleren gir nyeste først).
    public void Fyll(List<RessursFormViewModel> alle)
    {
        if (Kategori != KategoriTildelt && Kategori != KategoriAlle)
        {
            Kategori = KategoriTilgjengelig;
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

        Liste = new List<RessursFormViewModel>();
        AntallTilgjengelig = 0;
        AntallTildelt = 0;
        AntallAlle = 0;

        foreach (RessursFormViewModel ressurs in alle)
        {
            bool passer = (string.IsNullOrWhiteSpace(Type) || ressurs.Type == Type)
                && (kommunenummer == null || ressurs.Kommunenummer == kommunenummer);
            if (!passer)
            {
                continue;
            }

            // «Tilgjengelig» er ledig. Alt annet (i dag bare «Tildelt») er i bruk.
            string kategori = ressurs.Status == "Tilgjengelig" ? KategoriTilgjengelig : KategoriTildelt;
            AntallAlle++;
            if (kategori == KategoriTilgjengelig) AntallTilgjengelig++;
            if (kategori == KategoriTildelt) AntallTildelt++;

            if (Kategori == KategoriAlle || Kategori == kategori)
            {
                Liste.Add(ressurs);
            }
        }
    }
}
