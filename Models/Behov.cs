using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.Models;

// Hva slags hjelp som trengs. Navnene er på engelsk i koden og vises på norsk
// eller engelsk med Visningstekst.Type.
public enum BehovType { Transport, Drone, Generator, Evakuering, Annet }

// Hvor mye det haster: akutt (Urgent) eller planlagt (Planned).
// Urgent har tallet 0, så akutte behov kommer først når vi sorterer på prioritet.
public enum BehovPrioritet { Urgent = 0, Planned = 1 }

// Statusen lagres som tall i databasen. Nye verdier legges derfor alltid
// til på slutten, så de gamle tallene (0–3) beholder betydningen sin.
public enum BehovStatus { New, UnderReview, Assigned, Resolved, Rejected, Withdrawn }

// Et behov for hjelp som en innbygger har meldt. Dette er raden slik den lagres i
// databasen (tabellen Behovsliste). Skjemaet brukeren fyller ut, er BehovViewModel.
// Reglene for hvilken status behovet kan gå til, står i BehovStatusflyt.
public class Behov
{
    public int Id { get; set; }
    public BehovType Type { get; set; }
    public string? Beskrivelse { get; set; }
    public string? AnnetType { get; set; }

    // Kommunenummeret, for eksempel «4202» for Grimstad (se Models/Kommuner.cs).
    // Valgfritt: fylles ut fra kartet eller av brukeren. Tomt for eldre behov.
    [MaxLength(4)]
    public string? Kommunenummer { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public BehovPrioritet Prioritet { get; set; }
    public BehovStatus Status { get; set; } = BehovStatus.New;
    public string? KontaktTelefon { get; set; }
    public string? KontaktEpost { get; set; }
    public DateTime OpprettetTid { get; set; } = DateTime.UtcNow;
    public string? OpprettetAvBrukerId { get; set; }
    public DateTime? SistEndretTid { get; set; }
    public string? SistEndretAvBrukerId { get; set; }
}
