using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.Models;

// Data fra registreringsskjemaet
public class RegisterViewModel
{
    [Required(ErrorMessage = "Skriv inn e-post")]
    [EmailAddress(ErrorMessage = "Ugyldig e-post")]
    public string Epost { get; set; } = "";

    [Required(ErrorMessage = "Skriv inn passord")]
    [MinLength(8, ErrorMessage = "Passordet må være minst 8 tegn")]
    [RegularExpression(@".*\d.*", ErrorMessage = "Passordet må ha minst ett tall")]
    [DataType(DataType.Password)]
    public string Passord { get; set; } = "";

    [Required(ErrorMessage = "Gjenta passordet")]
    [Compare("Passord", ErrorMessage = "Passordene er ikke like")]
    [DataType(DataType.Password)]
    [Display(Name = "Gjenta passord")]
    public string BekreftPassord { get; set; } = "";
}
