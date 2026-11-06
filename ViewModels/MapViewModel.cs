namespace Nabohjelp.ViewModels;

// Alt kartet på Kommandobro trenger: to lister med punkter,
// én for ressurser og én for behov.
public class MapViewModel
{
    public List<KartPunkt> Ressurser { get; set; } = new List<KartPunkt>();

    public List<KartPunkt> Behov { get; set; } = new List<KartPunkt>();
}

// Ett punkt (en prikk) på kartet. Brukes for både behov og ressurser.
// Teksten er ferdig oversatt i controlleren, så kart.js bare viser den.
//
// Boksen som åpnes når man trykker på prikken, ser slik ut:
//   Slag          «Behov» eller «Ressurs», liten tekst øverst
//   Tittel        for eksempel «Transport»
//   Merker        små fargede merker, for eksempel «Akutt» og «Ny»
//   Undertekst    for eksempel «Meldt 06.11.2026 19:42»
//   Beskrivelse   teksten brukeren skrev, i en egen boks
//   Linjer        telefon, e-post og annet, med etikett til venstre
//   Fremhevet     nærmeste ressurs, i en farget boks
//   Lenker        knapper, for eksempel «Finn ressurs»
public class KartPunkt
{
    public string Slag { get; set; } = "";

    public string Tittel { get; set; } = "";

    public double Breddegrad { get; set; }

    public double Lengdegrad { get; set; }

    // Fargen på prikken (CSS-klassen nal-...):
    //   "hjelp"    = akutt behov (rød)
    //   "planlagt" = planlagt behov (gul)
    //   "gi"       = tilgjengelig ressurs (grønn)
    //   "opptatt"  = ressurs som er tildelt eller ikke tilgjengelig (grå)
    public string Farge { get; set; } = "gi";

    public List<KartMerke> Merker { get; set; } = new List<KartMerke>();

    public string? Undertekst { get; set; }

    public string? Beskrivelse { get; set; }

    public List<KartLinje> Linjer { get; set; } = new List<KartLinje>();

    // Vises i en farget boks. Tom betyr at boksen ikke vises.
    public string? Fremhevet { get; set; }

    // true gir grønn boks (fant en ressurs), false gir grå.
    public bool FremhevetErFunnet { get; set; }

    public List<KartLenke> Lenker { get; set; } = new List<KartLenke>();

    // Legger til en linje, men bare hvis det finnes en verdi å vise.
    // lenke gjør verdien klikkbar, for eksempel «tel:+4712345678».
    public void LeggTilLinje(string etikett, string? verdi, string? lenke = null)
    {
        if (!string.IsNullOrWhiteSpace(verdi))
        {
            Linjer.Add(new KartLinje { Etikett = etikett, Verdi = verdi, Lenke = lenke });
        }
    }
}

// Et lite merke i boksen. Farge er CSS-klassen (merke-hjelp, merke-lys, merke-gi eller tom).
public class KartMerke
{
    public string Tekst { get; set; } = "";

    public string Farge { get; set; } = "";
}

public class KartLinje
{
    public string Etikett { get; set; } = "";

    public string Verdi { get; set; } = "";

    public string? Lenke { get; set; }
}

public class KartLenke
{
    public string Tekst { get; set; } = "";

    public string Adresse { get; set; } = "";
}
