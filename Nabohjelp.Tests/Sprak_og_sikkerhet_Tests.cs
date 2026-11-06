using System.Text.RegularExpressions;
using System.Xml.Linq;
using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for språkvalget (alle tekster har en engelsk oversettelse)
// og for sikkerhetsinnstillingene i Services/Sikkerhet.cs.
public class SprakOgSikkerhetTests
{
    // ===================
    // ----- Språk -----
    // ===================
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

        // Alle oversettelsene i resx-filen.
        var engelsk = XDocument.Load(Path.Combine(rot, "Resources", "SharedResource.en.resx"))
            .Descendants("data")
            .Select(d => (string?)d.Attribute("name"))
            .ToHashSet();

        // Alle tekster i koden som skrives som T["..."] eller Oversetter.Tekst("...").
        var monster = new Regex("(?:\\bT\\[|Oversetter\\.Tekst\\()\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
        var mangler = new List<string>();

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
                    if (tekst != "..." && !engelsk.Contains(tekst))
                    {
                        mangler.Add(Path.GetFileName(fil) + ": " + tekst);
                    }
                }
            }
        }

        // Feiler testen, står tekstene som mangler engelsk oversettelse i meldingen.
        Assert.Empty(mangler.Distinct());
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

    // =========================
    // ----- Sikkerhet -----
    // =========================
    [Fact]
    public void Innholdsregler_StopperInnrammingOgPlugins()
    {
        // Siden kan ikke vises inne i en annen nettside, og gamle plugins (Flash o.l.) er stengt.
        Assert.Contains("frame-ancestors 'none'", Sikkerhet.InnholdsRegler);
        Assert.Contains("object-src 'none'", Sikkerhet.InnholdsRegler);
        Assert.Contains("form-action 'self'", Sikkerhet.InnholdsRegler);
    }

    [Fact]
    public void Fartsgrense_Er10ForsokPerMinutt()
    {
        Assert.Equal(10, Sikkerhet.MaksForsokPerMinutt);
    }
}
