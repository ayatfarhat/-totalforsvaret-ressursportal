using Nabohjelp.Models;
using Nabohjelp.ViewModels;

namespace Nabohjelp.Services;

// Søket på nettsiden (/Sok). Leter i sidene, i «Spørsmål og svar» og i
// behov og ressurser. Hvem som får se hvilke treff, styrer SokController
// (samme regler som ellers: frivillige ser sitt eget, Admin og Kommune ser alt).
//
// Vi søker i teksten slik brukeren ser den (på valgt språk), så et søk etter
// «generator» finner «Aggregat» når siden er på engelsk.
public static class Nettstedsok
{
    // Hvem en side er for.
    public const string ForAlle = "alle";
    public const string ForAnsatte = "ansatt";
    public const string ForAdmin = "admin";

    // Sidene som kan søkes opp: tittel, kort beskrivelse, adresse og hvem den er for.
    // Tekstene oversettes når det søkes (se FinnSider).
    public static readonly List<SokTreff> Sider = new List<SokTreff>
    {
        Side("Forside", "Hva BeredskapsPortalen er, og hvordan den virker.", "/", ForAlle),
        Side("Ressurser og behov", "Velg om du trenger hjelp eller kan hjelpe, og se ressursene dine.", "/Ressurs", ForAlle),
        Side("Meld behov", "Be om hjelp, for eksempel transport, strøm eller evakuering.", "/Behov/Opprett", ForAlle),
        Side("Mine behov", "Se, endre eller trekk tilbake behov du har meldt.", "/Behov/MineBehov", ForAlle),
        Side("Registrer en ressurs", "Registrer utstyr, kjøretøy, lokaler eller innsats du kan stille med.", "/Ressurs/Create", ForAlle),
        Side("Ønskede ressurser", "Ressurser kommunen og kriseledelsen ønsker at innbyggere registrerer.", "/OnsketRessurs", ForAlle),
        Side("Min konto", "Bytt passord, slå på tofaktor, last ned dataene dine eller slett kontoen.", "/Account", ForAlle),
        Side("Spørsmål og svar", "Svar på det folk ofte lurer på om BeredskapsPortalen.", "/Home/Sporsmal", ForAlle),
        Side("Personvern", "Hvilke personopplysninger vi lagrer, hvorfor og hvor lenge.", "/Home/Personvern", ForAlle),
        Side("Vilkår for bruk", "Reglene for å bruke BeredskapsPortalen.", "/Home/Vilkar", ForAlle),
        Side("Cookies", "Hvilke cookies siden bruker, og hvorfor.", "/Home/Cookies", ForAlle),
        Side("Tilgjengelighet", "Hvordan siden fungerer med tastatur, skjermleser og på mobil, og hva som ikke virker godt nok ennå.", "/Home/Tilgjengelighet", ForAlle),
        Side("Kommandobro", "Kart over behov og ressurser for kriseledelsen.", "/Kommandobro", ForAnsatte),
        Side("Behovskø", "Alle meldte behov med kontaktinformasjon og status.", "/Behov/Ko", ForAnsatte),
        Side("Ressursliste", "Alle registrerte ressurser med kontaktinformasjon og status.", "/Ressurs/Oversikt", ForAnsatte),
        Side("Tildeling", "Koble et behov med en ressurs i nærheten.", "/Tildeling", ForAnsatte),
        Side("Skriv ut ressursliste per kommune", "Ressursliste for utskrift, sortert på fylke og kommune.", "/Ressurs/Utskrift", ForAnsatte),
        Side("Legg til ønsket ressurs", "Fortell innbyggerne hva dere trenger.", "/OnsketRessurs/Create", ForAnsatte),
        Side("Brukere", "Se brukerne og endre roller.", "/Admin", ForAdmin),
        Side("Invitasjoner", "Lag invitasjonslenker som gir en rolle.", "/Admin/Invitasjoner", ForAdmin)
    };

    private static SokTreff Side(string tittel, string tekst, string adresse, string forHvem)
    {
        return new SokTreff { Tittel = tittel, Tekst = tekst, Adresse = adresse, ForHvem = forHvem };
    }

    // Sjekker om alle ordene i søket finnes et sted i tekstene (store og små bokstaver er likt).
    // «aggregat grimstad» passer altså en ressurs som er et aggregat i Grimstad.
    public static bool Passer(string sok, List<string?> tekster)
    {
        string alt = "";
        foreach (string? tekst in tekster)
        {
            if (tekst != null)
            {
                alt = alt + " " + tekst.ToLowerInvariant();
            }
        }

        string[] ord = sok.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (ord.Length == 0)
        {
            return false;
        }

        foreach (string etOrd in ord)
        {
            if (!alt.Contains(etOrd))
            {
                return false;
            }
        }
        return true;
    }

    // Sidene som passer søket, og som brukeren har tilgang til.
    public static List<SokTreff> FinnSider(string sok, bool erAnsatt, bool erAdmin)
    {
        var treff = new List<SokTreff>();
        foreach (SokTreff side in Sider)
        {
            bool harTilgang = side.ForHvem == ForAlle
                || (side.ForHvem == ForAnsatte && erAnsatt)
                || (side.ForHvem == ForAdmin && erAdmin);

            string tittel = Oversetter.Tekst(side.Tittel);
            string tekst = Oversetter.Tekst(side.Tekst);

            if (harTilgang && Passer(sok, new List<string?> { tittel, tekst }))
            {
                treff.Add(new SokTreff { Tittel = tittel, Tekst = tekst, Adresse = side.Adresse });
            }
        }
        return treff;
    }

    // Spørsmålene som passer søket. Lenken går rett til spørsmålet på siden.
    public static List<SokTreff> FinnSporsmal(string sok)
    {
        var treff = new List<SokTreff>();
        for (int i = 0; i < VanligeSporsmal.Alle.Count; i++)
        {
            string sporsmal = Oversetter.Tekst(VanligeSporsmal.Alle[i].Sporsmal);
            string svar = Oversetter.Tekst(VanligeSporsmal.Alle[i].Svar);

            if (Passer(sok, new List<string?> { sporsmal, svar }))
            {
                treff.Add(new SokTreff { Tittel = sporsmal, Tekst = svar, Adresse = "/Home/Sporsmal#sporsmal-" + (i + 1) });
            }
        }
        return treff;
    }

    // Ressursene som passer. Controlleren sender bare inn ressurser brukeren får se.
    public static List<SokTreff> FinnRessurser(string sok, List<Ressurs> ressurser)
    {
        var treff = new List<SokTreff>();
        foreach (Ressurs ressurs in ressurser)
        {
            string type = Visningstekst.RessursType(ressurs.Type, ressurs.AnnetType);
            string kommune = Kommuner.VisNavn(ressurs.Kommunenummer);

            if (Passer(sok, new List<string?> { type, kommune, ressurs.Posisjon, ressurs.Beskrivelse, ressurs.Navn }))
            {
                treff.Add(new SokTreff
                {
                    Tittel = type,
                    Tekst = kommune + " · " + ressurs.Posisjon,
                    Adresse = "/Ressurs/Confirmation/" + ressurs.Id
                });
            }
        }
        return treff;
    }

    // Behovene som passer. Controlleren sender bare inn behov brukeren får se.
    public static List<SokTreff> FinnBehov(string sok, List<Behov> behovsliste)
    {
        var treff = new List<SokTreff>();
        foreach (Behov behov in behovsliste)
        {
            string type = Visningstekst.Type(behov.Type, behov.AnnetType);
            string status = Visningstekst.Status(behov.Status);
            string prioritet = Visningstekst.Prioritet(behov.Prioritet);
            string kommune = Kommuner.VisNavn(behov.Kommunenummer);

            if (Passer(sok, new List<string?> { type, status, prioritet, kommune, behov.Beskrivelse }))
            {
                treff.Add(new SokTreff
                {
                    Tittel = type,
                    Tekst = prioritet + " · " + status + " · " + behov.OpprettetTid.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    Adresse = "/Behov/MineBehov#behov-" + behov.Id
                });
            }
        }
        return treff;
    }
}
