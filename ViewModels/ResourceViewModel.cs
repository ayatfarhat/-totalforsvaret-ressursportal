namespace Nabohjelp.ViewModels;

// ViewModel for en ressurs
public class ResourceViewModel
{
    // Navn på ressursen
    public string Name { get; set; } = string.Empty;

    // Beskrivelse av ressursen
    public string Description { get; set; } = string.Empty;

    // Kategori for ressursen
    public string Category { get; set; } = string.Empty;

    // Antall tilgjengelige
    public int Quantity { get; set; }

    // Om ressursen er tilgjengelig
    public bool Available { get; set; }

    // Plassering på kartet
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}