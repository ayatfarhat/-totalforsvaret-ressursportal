using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.Models;

public enum BehovType { Transport, Drone, Generator, Evakuering, Annet }

public enum BehovPrioritet { Urgent = 0, Planned = 1 }

// Statusen lagres som tall i databasen. Nye verdier legges derfor alltid
// til på slutten, så de gamle tallene (0–3) beholder betydningen sin.
public enum BehovStatus { New, UnderReview, Assigned, Resolved, Rejected, Withdrawn }

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
