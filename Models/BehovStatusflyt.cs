namespace Nabohjelp.Models;

// Reglene for statusflyten til et behov, samlet på ett sted:
//
//   Ny → Under vurdering → Tildelt → Løst
//    \          \
//     → Avvist   → Avvist                (Admin/Kommune, for eksempel duplikat eller feil)
//
//   Ny / Under vurdering → Trukket tilbake  (den som meldte behovet)
//
// Så lenge behovet er Ny eller Under vurdering, kan det også endres
// av den som meldte det, eller av Admin/Kommune.
//
// «Tildelt» settes av tildelingen (TildelingController), ikke med en knapp.
// Løst, Avvist og Trukket tilbake er avsluttet: da skjer det ikke mer med behovet.
public static class BehovStatusflyt
{
    // Stegene som vises i statuslinjen på «Mine behov».
    public static readonly BehovStatus[] Steg =
    {
        BehovStatus.New,
        BehovStatus.UnderReview,
        BehovStatus.Assigned,
        BehovStatus.Resolved
    };

    // Statusene Admin og Kommune kan flytte et behov til med en knapp i køen.
    public static List<BehovStatus> NesteStatuser(BehovStatus fra)
    {
        switch (fra)
        {
            case BehovStatus.New:
                return new List<BehovStatus> { BehovStatus.UnderReview, BehovStatus.Rejected };
            case BehovStatus.UnderReview:
                return new List<BehovStatus> { BehovStatus.Resolved, BehovStatus.Rejected };
            case BehovStatus.Assigned:
                return new List<BehovStatus> { BehovStatus.Resolved };
            default:
                return new List<BehovStatus>();
        }
    }

    public static bool KanEndres(BehovStatus fra, BehovStatus til)
    {
        return NesteStatuser(fra).Contains(til);
    }

    // Den som meldte behovet kan trekke det tilbake så lenge ingen ressurs er tildelt.
    public static bool KanTrekkesTilbake(BehovStatus status)
    {
        return status == BehovStatus.New || status == BehovStatus.UnderReview;
    }

    // Et behov kan endres (type, hast, posisjon, beskrivelse, kontaktinfo) så lenge
    // ingen ressurs er tildelt. Etter tildeling har kommunen planlagt ut fra det som står.
    public static bool KanRedigeres(BehovStatus status)
    {
        return status == BehovStatus.New || status == BehovStatus.UnderReview;
    }

    // Løst, Avvist og Trukket tilbake er avsluttet: da skjer det ikke mer med behovet.
    public static bool ErAvsluttet(BehovStatus status)
    {
        return status == BehovStatus.Resolved ||
               status == BehovStatus.Rejected ||
               status == BehovStatus.Withdrawn;
    }

    // Hvor langt behovet har kommet i statuslinjen: 0 = Ny ... 3 = Løst.
    // Gir -1 for Avvist og Trukket tilbake, som ikke er steg i linjen.
    public static int StegNummer(BehovStatus status)
    {
        return Array.IndexOf(Steg, status);
    }
}
