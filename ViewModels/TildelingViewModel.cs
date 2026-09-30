using Nabohjelp.Models;

namespace Nabohjelp.ViewModels;

// ViewModel for Ayats matching- og tildelingsside.
public class TildelingViewModel
{
    // Behovet som skal behandles.
    public int BehovId { get; set; }

    public BehovType BehovType { get; set; }

    public BehovPrioritet Prioritet { get; set; }

    public BehovStatus Status { get; set; }

    public string? Beskrivelse { get; set; }

    // Behovets plassering på kartet.
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    // Ressurser som matcher behovet.
    public List<MatchendeRessursViewModel> MatchendeRessurser { get; set; } = new();
}

// Representerer én ressurs som kan matches mot behovet.
public class MatchendeRessursViewModel
{
    // Indeks i den midlertidige ressurslisten.
    public int RessursId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Posisjon { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Navn { get; set; }

    public string? Beskrivelse { get; set; }

    // Forklarer hvorfor ressursen ble valgt som match.
    public string MatchBegrunnelse { get; set; } = string.Empty;
}