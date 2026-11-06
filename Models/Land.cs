namespace Nabohjelp.Models;

// Et land med landskode for telefonnummer, for eksempel Norge og +47.
// Brukes i nedtrekkslisten i skjemaene for behov og ressurser.
public class Land
{
    public string Navn { get; set; } = "";

    public string Kode { get; set; } = "";

    // Landskodene som kan velges. Den første i listen (+47) er standard.
    public static readonly List<Land> Alle = new List<Land>
    {
        new Land { Navn = "Norge", Kode = "+47" },
        new Land { Navn = "Sverige", Kode = "+46" },
        new Land { Navn = "Danmark", Kode = "+45" },
        new Land { Navn = "Finland", Kode = "+358" },
        new Land { Navn = "Island", Kode = "+354" },
        new Land { Navn = "Storbritannia", Kode = "+44" },
        new Land { Navn = "Tyskland", Kode = "+49" },
        new Land { Navn = "Polen", Kode = "+48" }
    };

    // Sjekker at koden finnes i listen over, så ingen kan sende inn en annen kode.
    public static bool KodeFinnes(string? kode)
    {
        foreach (Land land in Alle)
        {
            if (land.Kode == kode)
            {
                return true;
            }
        }
        return false;
    }
}
