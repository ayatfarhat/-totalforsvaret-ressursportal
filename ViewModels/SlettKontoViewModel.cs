using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.ViewModels;

// Skjemaet på «Slett konto»: brukeren bekrefter med passordet sitt.
// (Brukere som logger inn med Vipps eller Apple har ikke passord, se AccountController.SlettKonto.)
public class SlettKontoViewModel
{
    [Required(ErrorMessage = "Du må skrive inn passordet ditt.")]
    [DataType(DataType.Password)]
    public string Passord { get; set; } = string.Empty;
}