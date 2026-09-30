namespace Nabohjelp.ViewModels;

// ViewModel for en nødsituasjon
public class EmergencyViewModel
{
    // Tittel på nødsituasjonen
    public string Title { get; set; } = string.Empty;

    // Beskrivelse av nødsituasjonen
    public string Description { get; set; } = string.Empty;

    // Type nødsituasjon
    public string EmergencyType { get; set; } = string.Empty;

    // Plassering på kartet
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}