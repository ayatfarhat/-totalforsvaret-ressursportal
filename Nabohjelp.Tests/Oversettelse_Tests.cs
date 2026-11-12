using System.Text.RegularExpressions;
using System.Xml.Linq;
using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for språkvalget: alle tekster har en engelsk oversettelse.
// (Sikkerhetshodene og fartsgrensen testes i Systemtester.cs, mot appen som kjører.)
public class OversettelseTests
{
    [Fact]
    public void Oversetter_UtenOversettelser_GirNorskTekstMedVerdier()
    {
        // I enhetstestene er Oversetter.Localizer ikke satt, så teksten kommer tilbake på norsk.
        Assert.Equal("Antall må være et tall.", Oversetter.Tekst("{0} må være et tall.", "Antall"));
    }

    [Fact]
    public void AlleTeksterISidene_HarEngelskOversettelse()
    {
        string rot = FinnProsjektmappe();
        HashSet<string> engelsk = LesEngelskeOversettelser(rot);

        // Tekstene i koden, og tekstene som står i lister (spørsmål og svar, sidene i søket).
        List<string> tekster = FinnTeksterIKoden(rot);
        tekster.AddRange(TeksterILister());

        // Samler tekstene som mangler engelsk oversettelse. Feiler testen, står de i meldingen.
        var mangler = new List<string>();
        foreach (string tekst in tekster)
        {
            if (!engelsk.Contains(tekst) && !mangler.Contains(tekst))
            {
                mangler.Add(tekst);
            }
        }

        Assert.Empty(mangler);
    }

    // Leser alle norske tekster som har en engelsk oversettelse i resx-filen.
    // Filen er XML, der hver oversettelse står som <data name="norsk tekst">.
    private static HashSet<string> LesEngelskeOversettelser(string rot)
    {
        XDocument fil = XDocument.Load(Path.Combine(rot, "Resources", "SharedResource.en.resx"));

        var norskeTekster = new HashSet<string>();
        foreach (XElement oversettelse in fil.Descendants("data"))
        {
            XAttribute? navn = oversettelse.Attribute("name");
            if (navn != null)
            {
                norskeTekster.Add(navn.Value);
            }
        }
        return norskeTekster;
    }

    // Finner alle tekster i koden som skrives som T["..."] eller Oversetter.Tekst("...").
    private static List<string> FinnTeksterIKoden(string rot)
    {
        // Mønsteret finner teksten mellom anførselstegnene etter T[ eller Oversetter.Tekst(
        var monster = new Regex("(?:\\bT\\[|Oversetter\\.Tekst\\()\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
        var tekster = new List<string>();

        foreach (string mappe in new[] { "Views", "Controllers", "Models", "ViewModels", "Services" })
        {
            foreach (string fil in Directory.GetFiles(Path.Combine(rot, mappe), "*.*", SearchOption.AllDirectories))
            {
                if (!fil.EndsWith(".cshtml") && !fil.EndsWith(".cs"))
                {
                    continue;
                }

                foreach (Match treff in monster.Matches(File.ReadAllText(fil)))
                {
                    string tekst = treff.Groups[1].Value.Replace("\\\"", "\"");

                    // «...» står bare i kommentarer som eksempel.
                    if (tekst != "...")
                    {
                        tekster.Add(tekst);
                    }
                }
            }
        }
        return tekster;
    }

    // Spørsmål og svar og sidene i søket står i lister, ikke som T["..."].
    private static List<string> TeksterILister()
    {
        var tekster = new List<string>();
        foreach (Models.VanligSporsmal sporsmal in Models.VanligeSporsmal.Alle)
        {
            tekster.Add(sporsmal.Sporsmal);
            tekster.Add(sporsmal.Svar);
        }
        foreach (ViewModels.SokTreff side in Nettstedsok.Sider)
        {
            tekster.Add(side.Tittel);
            tekster.Add(side.Tekst);
        }
        return tekster;
    }

    // Finner mappen med Nabohjelp.csproj, ved å gå oppover fra der testene kjører.
    private static string FinnProsjektmappe()
    {
        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var mappe = new DirectoryInfo(start);
            while (mappe != null)
            {
                if (File.Exists(Path.Combine(mappe.FullName, "Nabohjelp.csproj")))
                {
                    return mappe.FullName;
                }
                mappe = mappe.Parent;
            }
        }

        throw new DirectoryNotFoundException("Fant ikke Nabohjelp.csproj.");
    }
}
