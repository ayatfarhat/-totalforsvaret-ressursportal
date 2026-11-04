using Nabohjelp.Models;

namespace Nabohjelp.ViewModels;

// Ressurslisten som kriseledelsen kan skrive ut, gruppert per kommune.
// Logikken ligger her (ikke i View-en), så den kan testes.
public class RessursUtskriftViewModel
{
    // Filteret som er valgt. Tomt betyr «alle».
    public string? ValgtFylke { get; set; }
    public string? ValgtKommune { get; set; }

    // Når listen ble laget. Vises øverst på utskriften.
    public DateTime Laget { get; set; }

    public List<KommuneGruppe> Grupper { get; set; } = new();

    public int Antall => Grupper.Sum(g => g.Ressurser.Count);

    // Teksten som forteller hva utskriften gjelder, for eksempel «Grimstad (Agder)».
    public string VisOmrade()
    {
        var kommune = Kommuner.Finn(ValgtKommune);
        if (kommune != null)
        {
            return $"{kommune.Navn} ({kommune.Fylke})";
        }

        if (!string.IsNullOrEmpty(ValgtFylke))
        {
            return ValgtFylke;
        }

        return "Hele landet";
    }

    // Lager utskriften. En gyldig kommune går foran fylket.
    // Ressurser uten kommune kommer sist, og bare når det ikke er filtrert.
    public static RessursUtskriftViewModel Lag(
        IEnumerable<RessursFormViewModel> ressurser,
        string? fylke,
        string? kommune,
        DateTime laget)
    {
        var valgtKommune = Kommuner.Finn(kommune);
        string? valgtFylke = fylke != null && Kommuner.Fylker.Contains(fylke) ? fylke : null;

        if (valgtKommune != null)
        {
            valgtFylke = valgtKommune.Fylke;
        }

        var utvalg = ressurser.Where(r =>
        {
            var k = Kommuner.Finn(r.Kommunenummer);

            if (valgtKommune != null)
            {
                return k != null && k.Nummer == valgtKommune.Nummer;
            }

            if (valgtFylke != null)
            {
                return k != null && k.Fylke == valgtFylke;
            }

            return true;
        });

        // Kommunenummeret følger fylkene, så sortering på nummer gir fylke for fylke.
        // Ressurser uten kommune får «9999» og havner sist.
        var grupper = utvalg
            .GroupBy(r => Kommuner.Finn(r.Kommunenummer)?.Nummer ?? "9999")
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var k = Kommuner.Finn(g.Key);
                return new KommuneGruppe
                {
                    Kommunenummer = k?.Nummer,
                    Kommune = k?.Navn ?? Kommuner.IkkeOppgitt,
                    Fylke = k?.Fylke ?? "",
                    Ressurser = g
                        .OrderBy(r => r.Type)
                        .ThenBy(r => r.Posisjon)
                        .ToList()
                };
            })
            .ToList();

        return new RessursUtskriftViewModel
        {
            ValgtFylke = valgtFylke,
            ValgtKommune = valgtKommune?.Nummer,
            Laget = laget,
            Grupper = grupper
        };
    }
}

public class KommuneGruppe
{
    public string? Kommunenummer { get; set; }
    public string Kommune { get; set; } = "";
    public string Fylke { get; set; } = "";
    public List<RessursFormViewModel> Ressurser { get; set; } = new();
}
