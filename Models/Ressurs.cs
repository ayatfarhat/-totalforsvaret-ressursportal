namespace Nabohjelp.Models;

public class Ressurs
{
    public int Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Posisjon { get; set; } = string.Empty;

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