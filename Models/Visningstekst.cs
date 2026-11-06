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

    // Som Type, men viser også det brukeren skrev når typen er «Annet»,
    // for eksempel «Annet: vannlekkasje».
    public static string Type(BehovType type, string? annetType)
    {
        if (type == BehovType.Annet && !string.IsNullOrWhiteSpace(annetType))
        {
            return "Annet: " + annetType.Trim();
        }
        return Type(type);
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
            case BehovStatus.Rejected: return "Avvist";
            case BehovStatus.Withdrawn: return "Trukket tilbake";
            default: return "Løst";
        }
    }

    // CSS-klassen for statusmerket (se .merke-* i site.css):
    // gul = under arbeid, grønn = hjelp er på vei eller gitt, rød = avvist.
    public static string StatusFarge(BehovStatus status)
    {
        switch (status)
        {
            case BehovStatus.UnderReview: return "merke-lys";
            case BehovStatus.Assigned: return "merke-gi";
            case BehovStatus.Resolved: return "merke-gi";
            case BehovStatus.Rejected: return "merke-hjelp";
            default: return "";
        }
    }

    // Ressurser har status som tekst. «Tilgjengelig» vises grønt.
    public static string RessursStatusFarge(string status)
    {
        if (status == "Tilgjengelig")
        {
            return "merke-gi";
        }
        return "";
    }
}
