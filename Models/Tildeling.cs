using Microsoft.AspNetCore.Identity;

namespace Nabohjelp.Models;

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
}