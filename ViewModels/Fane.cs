namespace Nabohjelp.ViewModels;

// Én fane over en liste, for eksempel «Åpne 4» i behovskøen.
// Fanene lages av BehovListeViewModel.Faner() og RessursListeViewModel.Faner(),
// og sidene går gjennom dem med foreach.
public class Fane
{
    // Verdien som står i adressen når fanen er valgt, for eksempel ?kategori=apne.
    public string Kategori { get; set; } = "";

    // Teksten på fanen, på valgt språk.
    public string Tekst { get; set; } = "";

    // Tallet ved siden av teksten: hvor mange som ligger i kategorien.
    public int Antall { get; set; }

    // Sant for fanen som vises nå.
    public bool ErValgt { get; set; }
}
