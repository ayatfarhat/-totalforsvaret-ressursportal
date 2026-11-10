using Nabohjelp.Services;

namespace Nabohjelp.ViewModels;

// Resultatet av et søk på nettsiden (/Sok), delt inn i grupper.
public class SokViewModel
{
    // Det brukeren skrev i søkefeltet.
    public string Sok { get; set; } = "";

    public List<SokTreff> Sider { get; set; } = new List<SokTreff>();

    public List<SokTreff> Sporsmal { get; set; } = new List<SokTreff>();

    public List<SokTreff> Ressurser { get; set; } = new List<SokTreff>();

    public List<SokTreff> Behov { get; set; } = new List<SokTreff>();

    // Frivillige får treff i sine egne registreringer, Admin og Kommune i alle.
    public bool ErAnsatt { get; set; }

    public bool ErInnlogget { get; set; }

    public int AntallTreff
    {
        get { return Sider.Count + Sporsmal.Count + Ressurser.Count + Behov.Count; }
    }

    // Gruppene i den rekkefølgen de vises på siden, med overskrift.
    public List<SokGruppe> Grupper()
    {
        return new List<SokGruppe>
        {
            new SokGruppe { Overskrift = Oversetter.Tekst("Sider"), Treff = Sider },
            new SokGruppe { Overskrift = Oversetter.Tekst("Spørsmål og svar"), Treff = Sporsmal },
            new SokGruppe { Overskrift = Oversetter.Tekst("Behov"), Treff = Behov },
            new SokGruppe { Overskrift = Oversetter.Tekst("Ressurser"), Treff = Ressurser }
        };
    }
}

// En gruppe med treff, for eksempel «Sider».
public class SokGruppe
{
    public string Overskrift { get; set; } = "";

    public List<SokTreff> Treff { get; set; } = new List<SokTreff>();
}

// Ett treff: en lenke med tittel og en kort tekst under.
public class SokTreff
{
    public string Tittel { get; set; } = "";

    public string Tekst { get; set; } = "";

    public string Adresse { get; set; } = "";

    // Brukes bare for sidene: hvem siden er for (se Services/Nettstedsok.cs).
    public string ForHvem { get; set; } = "";
}
