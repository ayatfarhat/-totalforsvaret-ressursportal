namespace Nabohjelp.Models;

// Gjør om verdiene i enum-ene (som er på engelsk i koden) til norsk
// tekst som kan vises til brukeren, og velger farge på merkelappen.
// Samles her, slik at alle sider viser samme ord og samme farger.
public static class Visningstekst
{
    public static string Type(BehovType type)
    {
        switch (type)
        {
            case BehovType.Transport: return "Transport";
            case BehovType.Drone: return "Drone";
            case BehovType.Generator: return "Aggregat";
            case BehovType.Evakuering: return "Evakuering";
            default: return "Annet";
        }
    }

    public static string Prioritet(BehovPrioritet prioritet)
    {
        if (prioritet == BehovPrioritet.Urgent)
        {
            return "Akutt";
        }
        return "Planlagt";
    }

    public static string Status(BehovStatus status)
    {
        switch (status)
        {
            case BehovStatus.New: return "Ny";
            case BehovStatus.UnderReview: return "Under vurdering";
            case BehovStatus.Assigned: return "Tildelt";
            default: return "Løst";
        }
    }

    // CSS-klasse for merkelappen (se .merke-* i site.css)
    public static string PrioritetFarge(BehovPrioritet prioritet)
    {
        if (prioritet == BehovPrioritet.Urgent)
        {
            return "merke-rod";
        }
        return "merke-bla";
    }

    public static string StatusFarge(BehovStatus status)
    {
        switch (status)
        {
            case BehovStatus.New: return "";
            case BehovStatus.UnderReview: return "merke-gul";
            case BehovStatus.Assigned: return "merke-bla";
            default: return "merke-gronn";
        }
    }

    // Akutte behov får en rød strek i tabellen (se .rad-akutt i site.css)
    public static string RadKlasse(BehovPrioritet prioritet)
    {
        if (prioritet == BehovPrioritet.Urgent)
        {
            return "rad-akutt";
        }
        return "";
    }

    // Lenken i menyen får klassen "aktiv" hvis brukeren er på den siden,
    // slik at den blir markert med en blå strek.
    public static string MenyKlasse(string aktivSide, string side)
    {
        if (aktivSide == side)
        {
            return "nav-link aktiv";
        }
        return "nav-link";
    }

    // Ressurser har status som tekst ("Tilgjengelig", "Tildelt" ...)
    public static string RessursStatusFarge(string status)
    {
        if (status == "Tilgjengelig")
        {
            return "merke-gronn";
        }
        if (status == "Tildelt")
        {
            return "merke-bla";
        }
        return "";
    }
}
