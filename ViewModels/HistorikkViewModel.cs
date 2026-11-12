using Nabohjelp.Models;
using Nabohjelp.Services;

namespace Nabohjelp.ViewModels;

// Historikken til ett behov, slik Views/Behov/_Historikk.cshtml viser den:
// først når behovet ble meldt, så hver statusendring (se Models/StatusEndring.cs).
public class HistorikkViewModel
{
    public Behov Behov { get; set; } = null!;

    public List<StatusEndring> Endringer { get; set; } = new List<StatusEndring>();

    // E-postadressen til dem som gjorde endringene. Tom for frivillige, som ikke
    // skal se hvem i kommunen som gjorde hva.
    public Dictionary<string, string> Brukernavn { get; set; } = new Dictionary<string, string>();

    // Hvem som gjorde endringen, eller null hvis det ikke skal vises.
    public string? Hvem(StatusEndring endring)
    {
        if (Brukernavn.Count == 0)
        {
            return null;
        }
        if (endring.EndretAvBrukerId != null && Brukernavn.ContainsKey(endring.EndretAvBrukerId))
        {
            return Brukernavn[endring.EndretAvBrukerId];
        }
        return Oversetter.Tekst("slettet konto");
    }
}

// En ressurs som er tildelt et behov nå. Behovskøen viser den med knappen «Angre tildeling».
public class TildeltRessursViewModel
{
    public int TildelingId { get; set; }

    public int BehovId { get; set; }

    public Ressurs Ressurs { get; set; } = null!;
}
