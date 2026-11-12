using System.Globalization;
using Nabohjelp.Models;
using Nabohjelp.Services;

namespace Nabohjelp.ViewModels;

// ViewModel for matching og tildeling.
public class TildelingViewModel
{
    public int BehovId { get; set; }

    public BehovType BehovType { get; set; }

    // Det brukeren skrev når behovstypen er «Annet».
    public string? AnnetType { get; set; }

    public BehovPrioritet Prioritet { get; set; }

    public BehovStatus Status { get; set; }

    public string? Beskrivelse { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public List<MatchendeRessursViewModel> MatchendeRessurser { get; set; } = new List<MatchendeRessursViewModel>();

    // Brukes bare på resultatsiden: tildelingen som kan angres. 0 = ingen knapp.
    public int TildelingId { get; set; }

    // Tekst som er enkel å vise i siden, så siden slipper egne if-setninger for tomme felt.
    public string VisBeskrivelse()
    {
        if (string.IsNullOrWhiteSpace(Beskrivelse))
        {
            return Oversetter.Tekst("Ingen beskrivelse");
        }
        return Beskrivelse;
    }
}

// Representerer én ressurs som matcher behovet.
public class MatchendeRessursViewModel
{
    // Ressursens ID i databasen.
    public int RessursId { get; set; }

    public string Type { get; set; } = string.Empty;

    // Det eieren skrev når typen er «Annet».
    public string? AnnetType { get; set; }

    public string Posisjon { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Navn { get; set; }

    public string? Beskrivelse { get; set; }

    // Avstand i kilometer. Null betyr at avstanden er ukjent.
    public double? AvstandKm { get; set; }

    public string MatchBegrunnelse { get; set; } = string.Empty;

    // Den nærmeste ressursen får merket «Anbefalt» (settes i TildelingController.Match).
    public bool ErAnbefalt { get; set; }

    // Avstanden slik den vises på kortet, for eksempel «2.4 km».
    public string VisAvstand()
    {
        if (AvstandKm == null)
        {
            return Oversetter.Tekst("Avstand ukjent");
        }
        return AvstandKm.Value.ToString("F1") + " km";
    }

    public string VisNavn()
    {
        if (string.IsNullOrWhiteSpace(Navn))
        {
            return Oversetter.Tekst("Ikke oppgitt");
        }
        return Navn;
    }

    // Avstanden som tall til avstandsfilteret på siden (data-avstand), med punktum
    // som desimaltegn slik JavaScript forventer. Tom tekst betyr at ressursen mangler posisjon.
    public string AvstandForFilter()
    {
        if (AvstandKm == null)
        {
            return "";
        }
        return AvstandKm.Value.ToString(CultureInfo.InvariantCulture);
    }
}