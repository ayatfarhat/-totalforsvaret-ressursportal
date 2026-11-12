namespace Nabohjelp.ViewModels;

// Ett varsel om at en ressurs er tildelt (se Services/Tildelingsvarsel.cs).
// Vises av Views/Shared/_Tildelingsvarsler.cshtml.
public class TildelingsvarselViewModel
{
    public int TildelingId { get; set; }

    // Ressursen, for eksempel «Aggregat».
    public string Ressurs { get; set; } = "";

    // Kommunen behovet er i, eller null hvis den ikke er kjent.
    public string? Kommune { get; set; }

    public DateTime Tid { get; set; }

    public string VisTid()
    {
        return Tid.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    }
}
