using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.Models;

// Data fra skjemaet "Endre passord" på Min konto.
public class EndrePassordViewModel
{
    [Required(ErrorMessage = "Skriv inn passordet du har nå")]
    [DataType(DataType.Password)]
    [Display(Name = "Nåværende passord")]
    public string NaavarendePassord { get; set; } = "";

    // Samme regler som ved registrering.
    [Required(ErrorMessage = "Skriv inn nytt passord")]
    [MinLength(8, ErrorMessage = "Passordet må være minst 8 tegn")]
    [RegularExpression(@"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).*$",
        ErrorMessage = "Passordet må ha stor og liten bokstav, tall og spesialtegn (for eksempel !)")]
    [DataType(DataType.Password)]
    [Display(Name = "Nytt passord")]
    public string NyttPassord { get; set; } = "";

    [Required(ErrorMessage = "Gjenta passordet")]
    [Compare("NyttPassord", ErrorMessage = "Passordene er ikke like")]
    [DataType(DataType.Password)]
    [Display(Name = "Gjenta nytt passord")]
    public string BekreftPassord { get; set; } = "";
}
