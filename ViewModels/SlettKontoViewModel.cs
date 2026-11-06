using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.ViewModels;

public class SlettKontoViewModel
{
    [Required(ErrorMessage = "Du må skrive inn passordet ditt.")]
    [DataType(DataType.Password)]
    public string Passord { get; set; } = string.Empty;
}