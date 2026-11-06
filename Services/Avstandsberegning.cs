namespace Nabohjelp.Services;

// Regner ut avstanden i luftlinje mellom et behov og en ressurs.
// Brukes i TildelingController for å vise de nærmeste ressursene først.
public static class Avstandsberegning
{
    // Jordas radius i kilometer.
    private const double JordradiusKm = 6371.0;

    // Gir avstanden i kilometer, eller null hvis en av posisjonene mangler eller er ugyldig.
    // Ressursens koordinater er decimal? fordi de lagres slik i databasen (se Models/Ressurs.cs).
    public static double? BeregnAvstandKm(
        double behovBredde,
        double behovLengde,
        decimal? ressursBredde,
        decimal? ressursLengde)
    {
        if (ressursBredde == null || ressursLengde == null)
        {
            return null;
        }

        double bredde2 = (double)ressursBredde.Value;
        double lengde2 = (double)ressursLengde.Value;

        if (!ErGyldigPosisjon(behovBredde, behovLengde) || !ErGyldigPosisjon(bredde2, lengde2))
        {
            return null;
        }

        // Haversine-formelen: regner ut avstanden langs jordoverflaten
        // ut fra forskjellen i breddegrad og lengdegrad (i radianer).
        double deltaBredde = TilRadianer(bredde2 - behovBredde);
        double deltaLengde = TilRadianer(lengde2 - behovLengde);

        double a =
            Math.Sin(deltaBredde / 2) * Math.Sin(deltaBredde / 2) +
            Math.Cos(TilRadianer(behovBredde)) * Math.Cos(TilRadianer(bredde2)) *
            Math.Sin(deltaLengde / 2) * Math.Sin(deltaLengde / 2);

        // Små avrundingsfeil kan gi en verdi rett over 1, som Math.Sqrt(1 - a) ikke tåler.
        a = Math.Min(a, 1.0);

        double vinkel = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return JordradiusKm * vinkel;
    }

    // En posisjon er gyldig når den ligger på jordkloden og ikke er 0,0
    // (0,0 betyr i praksis at ingen posisjon ble valgt).
    private static bool ErGyldigPosisjon(double bredde, double lengde)
    {
        if (double.IsNaN(bredde) || double.IsNaN(lengde))
        {
            return false;
        }

        if (bredde < -90 || bredde > 90 || lengde < -180 || lengde > 180)
        {
            return false;
        }

        return !(bredde == 0 && lengde == 0);
    }

    private static double TilRadianer(double grader)
    {
        return grader * Math.PI / 180.0;
    }
}
