using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.Models;

// En invitasjonslenke som Admin lager, for eksempel til en ny kommuneansatt.
// Den som åpner lenken og er logget inn, får rollen. Lenken kan brukes én gang
// og slutter å virke etter UtloperTid. Reglene står i Services/Invitasjonsregler.cs.
public class Invitasjon
{
    public int Id { get; set; }

    // Vi lagrer ikke selve koden fra lenken, bare en «hash» (et fingeravtrykk) av den.
    // Da kan ingen lage en gyldig lenke ut fra databasen, selv om de får lese den.
    [Required]
    [StringLength(64)]
    public string KodeHash { get; set; } = "";

    // Rollen brukeren får: Kommune eller Admin.
    [Required]
    [StringLength(20)]
    public string Rolle { get; set; } = "";

    public string? OpprettetAvBrukerId { get; set; }

    public DateTime OpprettetTid { get; set; } = DateTime.UtcNow;

    public DateTime UtloperTid { get; set; }

    // Fylles ut når lenken er brukt. Null betyr at den ikke er brukt ennå.
    public DateTime? BruktTid { get; set; }

    [StringLength(256)]
    public string? BruktAvEpost { get; set; }
}
