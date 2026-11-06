using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.ViewModels;

// Data fra registreringsskjemaet
public class RegisterViewModel
{
    [Required(ErrorMessage = "Skriv inn e-post")]
    [EmailAddress(ErrorMessage = "Ugyldig e-post")]
    public string Epost { get; set; } = "";

    [Required(ErrorMessage = "Skriv inn passord")]
    [MinLength(8, ErrorMessage = "Passordet må være minst 8 tegn")]
    // Samme regler som Identity bruker: stor og liten bokstav, tall og spesialtegn.
    // (?=...) betyr «et sted i teksten må det finnes ...».
    [RegularExpression(@"^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).*$",
        ErrorMessage = "Passordet må ha stor og liten bokstav, tall og spesialtegn (for eksempel !)")]
    [DataType(DataType.Password)]
    public string Passord { get; set; } = "";

    [Required(ErrorMessage = "Gjenta passordet")]
    [Compare("Passord", ErrorMessage = "Passordene er ikke like")]
    [DataType(DataType.Password)]
    [Display(Name = "Gjenta passord")]
    public string BekreftPassord { get; set; } = "";

    // GDPR: brukeren må krysse av for at hen har lest personvernerklæringen.
    // Sjekkes i AccountController.Register.
    public bool GodtarPersonvern { get; set; }
}
