using Nabohjelp.Models;

namespace Nabohjelp.Services;

// Reglene for hvilke ressurser som passer til et behov.
// Brukes av TildelingController (lista over ressurser å tildele)
// og KommandobroController (nærmeste ressurs i boksen på kartet).
public static class Ressursmatching
{
    // Bestemmer hvilke ressurstyper som passer til behovstypen.
    public static bool PasserTilBehov(BehovType behovType, string ressursType)
    {
        switch (behovType)
        {
            case BehovType.Drone:
                return ressursType == "Drone og droneoperatør";

            case BehovType.Generator:
                return ressursType == "Aggregat";

            case BehovType.Transport:
                return ressursType == "ATV" || ressursType == "Traktor" || ressursType == "Snøskuter"
                    || ressursType == "Lastebil" || ressursType == "Båt";

            case BehovType.Evakuering:
                return ressursType == "Mannskap/frivillige" || ressursType == "ATV" || ressursType == "Snøskuter"
                    || ressursType == "Lastebil" || ressursType == "Båt" || ressursType == "Lokaler";

            // Behovstypen «Annet» kan matches med alle ressurstyper.
            case BehovType.Annet:
                return true;

            default:
                return false;
        }
    }

    // Finner den nærmeste tilgjengelige ressursen som passer til behovet.
    // Ressurser uten posisjon tas ikke med, siden vi ikke vet hvor langt unna de er.
    // Gir null hvis ingen ressurs passer.
    public static NaermesteRessurs? FinnNaermeste(Behov behov, IEnumerable<Ressurs> ressurser)
    {
        NaermesteRessurs? naermeste = null;

        foreach (var ressurs in ressurser)
        {
            if (ressurs.Status != "Tilgjengelig" || !PasserTilBehov(behov.Type, ressurs.Type))
            {
                continue;
            }

            double? avstand = Avstandsberegning.BeregnAvstandKm(
                behov.Latitude, behov.Longitude, ressurs.Breddegrad, ressurs.Lengdegrad);

            if (avstand == null)
            {
                continue;
            }

            if (naermeste == null || avstand.Value < naermeste.AvstandKm)
            {
                naermeste = new NaermesteRessurs { Ressurs = ressurs, AvstandKm = avstand.Value };
            }
        }

        return naermeste;
    }
}

// Svaret fra FinnNaermeste: ressursen og hvor langt unna den er.
public class NaermesteRessurs
{
    public Ressurs Ressurs { get; set; } = null!;

    public double AvstandKm { get; set; }
}
