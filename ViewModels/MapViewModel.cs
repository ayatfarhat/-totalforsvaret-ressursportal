namespace Nabohjelp.ViewModels;

// Alt kartet på Kommandobro trenger: to lister med punkter,
// én for ressurser og én for behov.
public class MapViewModel
{
    public List<KartPunkt> Ressurser { get; set; } = new();

    public List<KartPunkt> Behov { get; set; } = new();
}

// Ett punkt (en prikk) på kartet. Brukes for både behov og ressurser.
// Teksten er ferdig oversatt i controlleren, så kart.js bare viser den.
public class KartPunkt
{
    // For eksempel «Behov: Aggregat» eller «Ressurs: Båt».
    public string Tittel { get; set; } = "";

    public double Breddegrad { get; set; }

    public double Lengdegrad { get; set; }

    // Fargen på prikken (CSS-klassen nal-...):
    //   "hjelp"    = akutt behov (rød)
    //   "planlagt" = planlagt behov (gul)
    //   "gi"       = tilgjengelig ressurs (grønn)
    //   "opptatt"  = ressurs som er tildelt eller ikke tilgjengelig (grå)
    public string Farge { get; set; } = "gi";

    // Linjene som vises når man trykker på prikken, for eksempel «Status: Ny».
    public List<KartLinje> Linjer { get; set; } = new();

    // Knapper i boksen, for eksempel «Finn ressurs».
    public List<KartLenke> Lenker { get; set; } = new();

    // Legger til en linje, men bare hvis det finnes en verdi å vise.
    public void LeggTilLinje(string etikett, string? verdi)
    {
        if (!string.IsNullOrWhiteSpace(verdi))
        {
            Linjer.Add(new KartLinje { Etikett = etikett, Verdi = verdi });
        }
    }
}

public class KartLinje
{
    public string Etikett { get; set; } = "";

    public string Verdi { get; set; } = "";
}

public class KartLenke
{
    public string Tekst { get; set; } = "";

    public string Adresse { get; set; } = "";
}
