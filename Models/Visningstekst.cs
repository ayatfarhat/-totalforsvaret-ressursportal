using System.Globalization;
using Nabohjelp.Services;

namespace Nabohjelp.Models;

// Gjør om verdiene i enum-ene (som er på engelsk i koden) til tekst som kan
// vises til brukeren, og velger farge på merkelappen. Teksten skrives på norsk her
// og oversettes til engelsk av Oversetter når brukeren har valgt engelsk.
// Samles her, slik at alle sider viser samme ord og samme farger.
public static class Visningstekst
{
    public static string Type(BehovType type)
    {
        switch (type)
        {
            case BehovType.Transport: return Oversetter.Tekst("Transport");
            case BehovType.Drone: return Oversetter.Tekst("Drone");
            case BehovType.Generator: return Oversetter.Tekst("Aggregat");
            case BehovType.Evakuering: return Oversetter.Tekst("Evakuering");
            default: return Oversetter.Tekst("Annet");
        }
    }

    // Som Type, men viser også det brukeren skrev når typen er «Annet»,
    // for eksempel «Annet: vannlekkasje».
    public static string Type(BehovType type, string? annetType)
    {
        if (type == BehovType.Annet && !string.IsNullOrWhiteSpace(annetType))
        {
            return Oversetter.Tekst("Annet") + ": " + annetType.Trim();
        }
        return Type(type);
    }

    public static string Prioritet(BehovPrioritet prioritet)
    {
        if (prioritet == BehovPrioritet.Urgent)
        {
            return Oversetter.Tekst("Akutt");
        }
        return Oversetter.Tekst("Planlagt");
    }

    // CSS-klassen for merket som viser hast: akutte behov får rødt merke.
    public static string PrioritetFarge(BehovPrioritet prioritet)
    {
        if (prioritet == BehovPrioritet.Urgent)
        {
            return "merke-hjelp";
        }
        return "";
    }

    public static string Status(BehovStatus status)
    {
        switch (status)
        {
            case BehovStatus.New: return Oversetter.Tekst("Ny");
            case BehovStatus.UnderReview: return Oversetter.Tekst("Under vurdering");
            case BehovStatus.Assigned: return Oversetter.Tekst("Tildelt");
            case BehovStatus.Rejected: return Oversetter.Tekst("Avvist");
            case BehovStatus.Withdrawn: return Oversetter.Tekst("Trukket tilbake");
            default: return Oversetter.Tekst("Løst");
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

    // CSS-klassen for ett steg i statuslinjen på «Mine behov» (Ny → Under vurdering → Tildelt → Løst).
    //   stegnummer: steget som tegnes (0 = Ny ... 3 = Løst)
    //   status:     statusen behovet har nå
    // Steg behovet er ferdig med, er grønne («ferdig»). Steget behovet står på, er
    // markert («na»). Et løst behov er ferdig helt ut, så siste steg er både grønt og markert.
    public static string StegKlasse(int stegnummer, BehovStatus status)
    {
        int stegNaa = BehovStatusflyt.StegNummer(status);

        if (stegnummer < stegNaa)
        {
            return "ferdig";
        }
        if (stegnummer > stegNaa)
        {
            return "";
        }
        if (status == BehovStatus.Resolved)
        {
            return "ferdig na";
        }
        return "na";
    }

    // En koordinat som tekst med punktum som desimaltegn, for eksempel «58.34051».
    // Kartet (kart.js) og kartlenker forstår ikke komma, som norsk tallformat bruker.
    public static string Koordinat(double verdi)
    {
        return verdi.ToString("0.#####", CultureInfo.InvariantCulture);
    }

    // Adressen som åpner et punkt i OpenStreetMap, med en nål på punktet.
    public static string KartLenke(double breddegrad, double lengdegrad)
    {
        string bredde = Koordinat(breddegrad);
        string lengde = Koordinat(lengdegrad);
        return "https://www.openstreetmap.org/?mlat=" + bredde + "&mlon=" + lengde
            + "#map=15/" + bredde + "/" + lengde;
    }

    // Ressurstypene er tekst (se RessursFormViewModel.Typer). Som for behov
    // vises det brukeren skrev når typen er «Annet», for eksempel «Annet: Vannpumpe».
    public static string RessursType(string type, string? annetType)
    {
        if (type == "Annet" && !string.IsNullOrWhiteSpace(annetType))
        {
            return Oversetter.Tekst("Annet") + ": " + annetType.Trim();
        }
        return Oversetter.Tekst(type);
    }

    // Teksten for én linje i historikken til et behov (se Models/StatusEndring.cs),
    // for eksempel «Under vurdering», «Tildelt: Aggregat» eller «Tildelingen av Aggregat ble angret».
    public static string Hendelse(StatusEndring endring)
    {
        if (endring.RessursType == null)
        {
            return Status(endring.TilStatus);
        }

        string ressurs = RessursType(endring.RessursType, endring.RessursAnnetType);
        if (endring.ErAngret)
        {
            return Oversetter.Tekst("Tildelingen av {0} ble angret", ressurs);
        }
        return Oversetter.Tekst("Tildelt: {0}", ressurs);
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
