using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.Models;

public class Ressurs
{
    public int Id { get; set; }

    public string Type { get; set; } = string.Empty;

    // Det brukeren skrev selv når typen er «Annet», for eksempel «Vannpumpe».
    [MaxLength(100)]
    public string? AnnetType { get; set; }

    public string Posisjon { get; set; } = string.Empty;

    // Kommunenummeret, for eksempel «4202» for Grimstad (se Models/Kommuner.cs).
    // Kan være tomt for ressurser som ble registrert før feltet fantes.
    [MaxLength(4)]
    public string? Kommunenummer { get; set; }

    public decimal? Breddegrad { get; set; }

    public decimal? Lengdegrad { get; set; }

    public DateTime? TilgjengeligFra { get; set; }

    public string? Beskrivelse { get; set; }

    public string? Navn { get; set; }

    public string? Telefon { get; set; }

    public string Status { get; set; } = "Tilgjengelig";

    public DateTime OpprettetTid { get; set; } = DateTime.UtcNow;

    public string? OpprettetAvBrukerId { get; set; }
}