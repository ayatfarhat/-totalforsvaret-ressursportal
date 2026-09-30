using System.ComponentModel.DataAnnotations;
using Kommandobro.Models;

namespace Kommandobro.ViewModels;
public class BehovViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Velg behovstype.")]
    [Display(Name = "Behovstype")]
    public BehovType? Type { get; set; }          

    [MaxLength(2000)]
    [Display(Name = "Beskrivelse")]
    public string? Beskrivelse { get; set; }

    [Required(ErrorMessage = "Breddegrad er påkrevd.")]
    [Range(-90, 90, ErrorMessage = "Breddegrad må være mellom -90 og 90.")]
    public double? Latitude { get; set; }

    [Required(ErrorMessage = "Lengdegrad er påkrevd.")]
    [Range(-180, 180, ErrorMessage = "Lengdegrad må være mellom -180 og 180.")]
    public double? Longitude { get; set; }

    [Required(ErrorMessage = "Velg prioritet.")]
    public BehovPrioritet? Prioritet { get; set; }

    [Phone(ErrorMessage = "Ugyldig telefonnummer.")]
    public string? KontaktTelefon { get; set; }

    [EmailAddress(ErrorMessage = "Ugyldig e-postadresse.")]
    public string? KontaktEpost { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(KontaktTelefon) && string.IsNullOrWhiteSpace(KontaktEpost))
            yield return new ValidationResult(
                "Oppgi telefonnummer eller e-post.",
                new[] { nameof(KontaktTelefon), nameof(KontaktEpost) });
    }
}
public class BehovKoItem
{
    public int Id { get; set; }
    public BehovType Type { get; set; }
    public BehovPrioritet Prioritet { get; set; }
    public BehovStatus Status { get; set; }
    public string? Beskrivelse { get; set; }
    public DateTime OpprettetTid { get; set; }
    public List<BehovStatus> MuligeNesteStatuser { get; set; } = new();
}
