using Nabohjelp.Models;

namespace Nabohjelp.ViewModels;

// ViewModel for matching og tildeling.
public class TildelingViewModel
{
    public int BehovId { get; set; }

    public BehovType BehovType { get; set; }

    public BehovPrioritet Prioritet { get; set; }

    public BehovStatus Status { get; set; }

    public string? Beskrivelse { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public List<MatchendeRessursViewModel> MatchendeRessurser { get; set; } = new();
}

// Representerer én ressurs som matcher behovet.
public class MatchendeRessursViewModel
{
    // Ressursens ID i databasen.
    public int RessursId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Posisjon { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Navn { get; set; }

    public string? Beskrivelse { get; set; }

    // Avstand i kilometer. Null betyr at avstanden er ukjent.
    public double? AvstandKm { get; set; }

    public string MatchBegrunnelse { get; set; } = string.Empty;
}