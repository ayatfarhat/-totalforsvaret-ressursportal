using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Models;

// Data fra innloggingsskjemaet
public class LoginViewModel
{
    [Required(ErrorMessage = "Skriv inn e-post")]
    [EmailAddress(ErrorMessage = "Ugyldig e-post")]
    public string Epost { get; set; } = "";

    [Required(ErrorMessage = "Skriv inn passord")]
    [DataType(DataType.Password)]
    public string Passord { get; set; } = "";
}
