using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.Models;

// Brukes både når man slår på 2FA og når man logger inn med 2FA
public class TofaViewModel
{
    // Den hemmelige nøkkelen som legges inn i autentiseringsappen (bare når man slår på 2FA)
    public string Nokkel { get; set; } = "";

    // Den 6-sifrede koden fra appen
    [Required(ErrorMessage = "Skriv inn koden fra appen")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Koden er 6 siffer")]
    public string Kode { get; set; } = "";
}
