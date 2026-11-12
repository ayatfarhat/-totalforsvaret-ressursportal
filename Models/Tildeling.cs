using Microsoft.AspNetCore.Identity;

namespace Nabohjelp.Models;

// En tildeling kobler et behov med en ressurs, og husker hvem som tildelte og når.
// Lages i TildelingController.Tildel.
//
// BehovId, RessursId og TildeltAvBrukerId er nøklene som lagres i tabellen.
// Behov, Ressurs og TildeltAvBruker er selve objektene, som Entity Framework kan hente
// ut fra nøklene. «= null!» betyr: «verdien settes av Entity Framework, ikke av oss»
// (uten det ville kompilatoren advare om at egenskapen kan være null).
public class Tildeling
{
    public int Id { get; set; }

    public int BehovId { get; set; }
    public Behov Behov { get; set; } = null!;

    public int RessursId { get; set; }
    public Ressurs Ressurs { get; set; } = null!;

    public string TildeltAvBrukerId { get; set; } = string.Empty;
    public IdentityUser TildeltAvBruker { get; set; } = null!;

    public DateTime TildeltTid { get; set; } = DateTime.UtcNow;

    // Om den som eier ressursen, har sett at ressursen er tildelt. Til da vises et
    // varsel på forsiden og under «Mine ressurser» (se Services/Tildelingsvarsel.cs).
    public bool EierHarSett { get; set; }
}