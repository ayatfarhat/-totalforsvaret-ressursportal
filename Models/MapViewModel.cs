namespace Nabohjelp.Models;

// Alt kartet trenger for å vise oversikten: to lister med punkter,
// én for ressurser og én for behov.
public class MapViewModel
{
    public List<KartPunkt> Ressurser { get; set; } = new();

    public List<KartPunkt> Behov { get; set; } = new();
}

// Ett punkt på kartet — brukes for både ressurser og behov,
// siden begge bare trenger en tittel og en posisjon her.
public class KartPunkt
{
    public string Tittel { get; set; } = "";

    public double Breddegrad { get; set; }

    public double Lengdegrad { get; set; }
}
