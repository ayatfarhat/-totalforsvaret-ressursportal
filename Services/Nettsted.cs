namespace Nabohjelp.Services;

// Felles opplysninger om nettsiden, brukt i flere sider.
public static class Nettsted
{
    // Når nettsiden sist ble oppdatert, vist nederst på alle sider. Vi bruker tidspunktet
    // programfilen ble bygget, så datoen endrer seg av seg selv ved hver nye versjon.
    public static readonly DateTime SistOppdatert = FinnByggedato();

    private static DateTime FinnByggedato()
    {
        string programfil = typeof(Nettsted).Assembly.Location;
        if (programfil == "" || !File.Exists(programfil))
        {
            return DateTime.Today;
        }
        return File.GetLastWriteTime(programfil);
    }

    // Navnet på cookien som husker at brukeren har trykket «OK» på cookie-informasjonen.
    public const string CookieInfoNavn = "BeredskapsPortalen.CookieInfo";
}
