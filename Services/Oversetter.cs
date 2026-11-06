using Microsoft.Extensions.Localization;

namespace Nabohjelp.Services;

// Oversetter tekst til språket brukeren har valgt (norsk eller engelsk).
//
// I sidene og i controllere som har T, bruker vi T["Norsk tekst"].
// Denne klassen er for kode som ikke har T: modeller, tjenester og noen controllere.
// Den bruker de samme oversettelsene (Resources/SharedResource.en.resx).
public static class Oversetter
{
    // Settes én gang i Program.cs. Er den ikke satt (for eksempel i enhetstestene),
    // kommer teksten tilbake på norsk.
    public static IStringLocalizer? Localizer { get; set; }

    // Gir teksten på valgt språk. Finnes ingen oversettelse, brukes den norske teksten.
    public static string Tekst(string norsk)
    {
        if (Localizer == null)
        {
            return norsk;
        }
        return Localizer[norsk].Value;
    }

    // Samme som over, men med verdier som settes inn i teksten, for eksempel
    // Tekst("{0} må være et tall.", "Antall") -> «Antall må være et tall.»
    public static string Tekst(string norsk, params object[] verdier)
    {
        return string.Format(Tekst(norsk), verdier);
    }

    // Oversetter flere linjer hver for seg og setter dem sammen med linjeskift.
    // Brukes til forslagene som fylles inn i beskrivelsesfeltet.
    public static string Linjer(params string[] linjer)
    {
        var oversatt = new List<string>();
        foreach (string linje in linjer)
        {
            oversatt.Add(Tekst(linje));
        }
        return string.Join("\n", oversatt);
    }
}
