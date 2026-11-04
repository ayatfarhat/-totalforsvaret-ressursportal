namespace Nabohjelp.Services;

public static class Avstandsberegning
{
    // Beregner luftlinjeavstand mellom to geografiske posisjoner.
    // Returnerer null dersom koordinatene mangler eller er ugyldige.
    public static double? BeregnAvstandKm(
        double behovLat,
        double behovLon,
        decimal? ressursLat,
        decimal? ressursLon)
    {
        if (!ressursLat.HasValue || !ressursLon.HasValue)
        {
            return null;
        }

        double lat2 = (double)ressursLat.Value;
        double lon2 = (double)ressursLon.Value;

        if (!double.IsFinite(behovLat) ||
            !double.IsFinite(behovLon) ||
            !double.IsFinite(lat2) ||
            !double.IsFinite(lon2) ||
            behovLat < -90 || behovLat > 90 ||
            lat2 < -90 || lat2 > 90 ||
            behovLon < -180 || behovLon > 180 ||
            lon2 < -180 || lon2 > 180 ||
            (behovLat == 0 && behovLon == 0) ||
            (lat2 == 0 && lon2 == 0))
        {
            return null;
        }

        const double jordradiusKm = 6371.0;
        double tilRadianer = Math.PI / 180.0;

        double deltaLat = (lat2 - behovLat) * tilRadianer;
        double deltaLon = (lon2 - behovLon) * tilRadianer;

        double a =
            Math.Pow(Math.Sin(deltaLat / 2), 2) +
            Math.Cos(behovLat * tilRadianer) *
            Math.Cos(lat2 * tilRadianer) *
            Math.Pow(Math.Sin(deltaLon / 2), 2);

        a = Math.Clamp(a, 0.0, 1.0);

        double c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));

        return jordradiusKm * c;
    }
}