using System.ComponentModel.DataAnnotations;
using Nabohjelp.Models;

namespace Nabohjelp.ViewModels;
public class BehovViewModel : IValidatableObject
{
    // 0 = et nytt behov. Ellers id-en til behovet som endres.
    public int Id { get; set; }

    [Required(ErrorMessage = "Velg behovstype.")]
    [Display(Name = "Behovstype")]
    public BehovType? Type { get; set; }          

    [MaxLength(2000)]
    [Display(Name = "Beskrivelse")]
    public string? Beskrivelse { get; set; }

    [MaxLength(100)]
    [Display(Name = "Hva slags behov?")]
    public string? AnnetType { get; set; }

    [Required(ErrorMessage = "Breddegrad er påkrevd.")]
    [Range(-90, 90, ErrorMessage = "Breddegrad må være mellom -90 og 90.")]
    public double? Latitude { get; set; }

    [Required(ErrorMessage = "Lengdegrad er påkrevd.")]
    [Range(-180, 180, ErrorMessage = "Lengdegrad må være mellom -180 og 180.")]
    public double? Longitude { get; set; }

    [Required(ErrorMessage = "Velg prioritet.")]
    public BehovPrioritet? Prioritet { get; set; }

    // Landskoden velges i en egen liste (samme liste som for ressurser, +47 er standard).
    public string Landskode { get; set; } = "+47";

    // Selve nummeret skrives uten landskode, bare tall (mellomrom er greit).
    [RegularExpression(@"^[0-9 ]{5,15}$",
        ErrorMessage = "Skriv telefonnummeret uten landskode, bare tall.")]
    public string? KontaktTelefon { get; set; }

    [EmailAddress(ErrorMessage = "Ugyldig e-postadresse.")]
    public string? KontaktEpost { get; set; }


    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Type == BehovType.Annet && string.IsNullOrWhiteSpace(AnnetType))
            yield return new ValidationResult(
                "Oppgi hva slags behov det er.",
                new[] { nameof(AnnetType) });

        if (string.IsNullOrWhiteSpace(KontaktTelefon) && string.IsNullOrWhiteSpace(KontaktEpost))
            yield return new ValidationResult(
                "Oppgi telefonnummer eller e-post.",
                new[] { nameof(KontaktTelefon), nameof(KontaktEpost) });

        if (!RessursFormViewModel.Landskoder.Any(l => l.Kode == Landskode))
            yield return new ValidationResult(
                "Velg en landskode fra listen.",
                new[] { nameof(Landskode) });
    }

    // I databasen lagres landskode og nummer sammen, for eksempel «+47 12345678».
    public string? TelefonForLagring()
    {
        if (string.IsNullOrWhiteSpace(KontaktTelefon))
        {
            return null;
        }
        return Landskode + " " + KontaktTelefon.Trim();
    }

    // Deler et lagret nummer («+47 12345678») opp i landskode og nummer igjen,
    // slik at skjemaet viser det riktig når behovet endres.
    public void LesTelefonFraLagring(string? lagret)
    {
        if (string.IsNullOrWhiteSpace(lagret))
        {
            KontaktTelefon = null;
            return;
        }

        lagret = lagret.Trim();
        foreach (var land in RessursFormViewModel.Landskoder)
        {
            if (lagret.StartsWith(land.Kode + " "))
            {
                Landskode = land.Kode;
                KontaktTelefon = lagret.Substring(land.Kode.Length + 1).Trim();
                return;
            }
        }

        // Nummer lagret før landskoden fantes. Et nummer med en ukjent landskode
        // vises som det er, så brukeren ser det og kan rette det (ingenting forsvinner).
        Landskode = "+47";
        KontaktTelefon = lagret;
    }

    // Fyller skjemaet med et behov fra databasen (brukes når behovet endres).
    public static BehovViewModel FraBehov(Behov behov)
    {
        var skjema = new BehovViewModel
        {
            Id = behov.Id,
            Type = behov.Type,
            Beskrivelse = behov.Beskrivelse,
            Latitude = behov.Latitude,
            Longitude = behov.Longitude,
            Prioritet = behov.Prioritet,
            KontaktEpost = behov.KontaktEpost
        };
        skjema.LesTelefonFraLagring(behov.KontaktTelefon);
        return skjema;
    }

    // Forslag til beskrivelse som fylles inn når brukeren velger behovstype.
    // Brukeren kan fritt endre teksten etterpå.
    public static string HentForslag(BehovType type)
    {
        switch (type)
        {
            case BehovType.Transport:
                return "Hva skal fraktes (personer eller utstyr):\nFra hvor og til hvor:\nNår trengs det:";
            case BehovType.Drone:
                return "Hva skal dronen brukes til (søk, oversikt, levering):\nOmråde:\nNår trengs det:";
            case BehovType.Generator:
                return "Hva trenger strøm (for eksempel medisinsk utstyr):\nOmtrent hvor mye effekt (kW), hvis du vet:\nHvor lenge:";
            case BehovType.Evakuering:
                return "Antall personer:\nTrenger noen spesiell hjelp (rullestol, medisiner):\nHvor skal de:";
            default:
                return "";
        }
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
    public string? KontaktTelefon { get; set; }
    public string? KontaktEpost { get; set; }
    public List<BehovStatus> MuligeNesteStatuser { get; set; } = new();
}
