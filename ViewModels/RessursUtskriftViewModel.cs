using Nabohjelp.Models;
using Nabohjelp.Services;

namespace Nabohjelp.ViewModels;

// Ressurslisten som kriseledelsen kan skrive ut, gruppert per kommune.
// Logikken ligger her (ikke i View-en), så den kan testes.
public class RessursUtskriftViewModel
{
    // Nummeret som brukes for ressurser uten kommune. Det er høyere enn alle
    // ekte kommunenumre, så disse ressursene havner sist i lista.
    private const string UtenKommune = "9999";

    // Filteret som er valgt. Tomt betyr «alle».
    public string? ValgtFylke { get; set; }
    public string? ValgtKommune { get; set; }

    // Når listen ble laget. Vises øverst på utskriften.
    public DateTime Laget { get; set; }

    public List<KommuneGruppe> Grupper { get; set; } = new List<KommuneGruppe>();

    // Antall ressurser i hele utskriften.
    public int Antall
    {
        get
        {
            int antall = 0;
            foreach (KommuneGruppe gruppe in Grupper)
            {
                antall = antall + gruppe.Ressurser.Count;
            }
            return antall;
        }
    }

    // Teksten som forteller hva utskriften gjelder, for eksempel «Grimstad (Agder)».
    public string VisOmrade()
    {
        Kommune? kommune = Kommuner.Finn(ValgtKommune);
        if (kommune != null)
        {
            return kommune.Navn + " (" + kommune.Fylke + ")";
        }

        if (!string.IsNullOrEmpty(ValgtFylke))
        {
            return ValgtFylke;
        }

        return Oversetter.Tekst("Hele landet");
    }

    // Lager utskriften i tre steg: velg ut, sorter og del i grupper per kommune.
    // En gyldig kommune går foran fylket. Ressurser uten kommune kommer sist,
    // og bare når det ikke er filtrert.
    public static RessursUtskriftViewModel Lag(
        List<RessursFormViewModel> ressurser,
        string? fylke,
        string? kommune,
        DateTime laget)
    {
        // Finner hva det er filtrert på. Ukjente verdier fra adressen ignoreres.
        Kommune? valgtKommune = Kommuner.Finn(kommune);

        string? valgtFylke = null;
        if (fylke != null && Kommuner.Fylker.Contains(fylke))
        {
            valgtFylke = fylke;
        }
        if (valgtKommune != null)
        {
            valgtFylke = valgtKommune.Fylke;
        }

        // 1. Velger ut ressursene som skal være med.
        var utvalg = new List<RessursFormViewModel>();
        foreach (RessursFormViewModel ressurs in ressurser)
        {
            if (ErMed(ressurs, valgtKommune, valgtFylke))
            {
                utvalg.Add(ressurs);
            }
        }

        // 2. Sorterer på kommunenummer, så type og så adresse.
        //    Kommunenummeret følger fylkene, så lista blir fylke for fylke.
        utvalg.Sort(SammenlignForUtskrift);

        // 3. Deler lista i grupper, én per kommune. Lista er sortert, så ressursene
        //    i samme kommune ligger etter hverandre. Ny kommune betyr ny gruppe.
        var grupper = new List<KommuneGruppe>();
        KommuneGruppe? gruppe = null;
        string forrigeNummer = "";

        foreach (RessursFormViewModel ressurs in utvalg)
        {
            string nummer = Gruppenummer(ressurs);
            if (gruppe == null || nummer != forrigeNummer)
            {
                gruppe = LagGruppe(nummer);
                grupper.Add(gruppe);
                forrigeNummer = nummer;
            }
            gruppe.Ressurser.Add(ressurs);
        }

        var modell = new RessursUtskriftViewModel
        {
            ValgtFylke = valgtFylke,
            Laget = laget,
            Grupper = grupper
        };
        if (valgtKommune != null)
        {
            modell.ValgtKommune = valgtKommune.Nummer;
        }
        return modell;
    }

    // Sjekker om ressursen hører til kommunen eller fylket det er filtrert på.
    private static bool ErMed(RessursFormViewModel ressurs, Kommune? valgtKommune, string? valgtFylke)
    {
        Kommune? kommune = Kommuner.Finn(ressurs.Kommunenummer);

        if (valgtKommune != null)
        {
            return kommune != null && kommune.Nummer == valgtKommune.Nummer;
        }

        if (valgtFylke != null)
        {
            return kommune != null && kommune.Fylke == valgtFylke;
        }

        // Ikke filtrert: alle er med.
        return true;
    }

    // Kommunenummeret ressursen sorteres og grupperes etter.
    private static string Gruppenummer(RessursFormViewModel ressurs)
    {
        Kommune? kommune = Kommuner.Finn(ressurs.Kommunenummer);
        if (kommune == null)
        {
            return UtenKommune;
        }
        return kommune.Nummer;
    }

    // Bestemmer rekkefølgen: kommunenummer, så type, så adresse.
    // Gir et negativt tall hvis a skal stå før b, og et positivt hvis b skal stå før a.
    private static int SammenlignForUtskrift(RessursFormViewModel a, RessursFormViewModel b)
    {
        int svar = string.Compare(Gruppenummer(a), Gruppenummer(b));
        if (svar != 0)
        {
            return svar;
        }

        svar = string.Compare(a.Type, b.Type);
        if (svar != 0)
        {
            return svar;
        }

        return string.Compare(a.Posisjon, b.Posisjon);
    }

    // Lager en tom gruppe med navnet på kommunen og fylket.
    private static KommuneGruppe LagGruppe(string nummer)
    {
        var gruppe = new KommuneGruppe();

        Kommune? kommune = Kommuner.Finn(nummer);
        if (kommune == null)
        {
            gruppe.Kommune = Kommuner.IkkeOppgitt;
        }
        else
        {
            gruppe.Kommunenummer = kommune.Nummer;
            gruppe.Kommune = kommune.Navn;
            gruppe.Fylke = kommune.Fylke;
        }

        return gruppe;
    }
}

// Ressursene i én kommune. Vises som én tabell i utskriften.
public class KommuneGruppe
{
    public string? Kommunenummer { get; set; }
    public string Kommune { get; set; } = "";
    public string Fylke { get; set; } = "";
    public List<RessursFormViewModel> Ressurser { get; set; } = new List<RessursFormViewModel>();
}
