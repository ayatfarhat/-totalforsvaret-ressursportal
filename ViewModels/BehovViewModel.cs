using System.ComponentModel.DataAnnotations;
using Nabohjelp.Models;
using Nabohjelp.Services;

namespace Nabohjelp.ViewModels;

// Skjemaet for å melde et nytt behov eller endre et behov (Views/Behov/Opprett.cshtml).
// Feltene er nullable (?), slik at «ikke valgt» kan vises som en feilmelding
// i stedet for at for eksempel typen blir «Transport» av seg selv.
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
[Range(typeof(double), "-90", "90",
    ErrorMessage = "Breddegrad må være mellom -90 og 90.")]
public double? Latitude { get; set; }

[Required(ErrorMessage = "Lengdegrad er påkrevd.")]
[Range(typeof(double), "-180", "180",
    ErrorMessage = "Lengdegrad må være mellom -180 og 180.")]
public double? Longitude { get; set; }

    [Required(ErrorMessage = "Velg prioritet.")]
    public BehovPrioritet? Prioritet { get; set; }

    // Kommunen, slik brukeren skrev den eller kartet fylte den inn (for eksempel «Grimstad»).
    // Valgfri. Gjøres om til kommunenummer i KopierTil.
    [MaxLength(60)]
    public string? KommuneNavn { get; set; }

    // Landskoden velges i en egen liste (se Models/Land.cs). +47 er standard.
    public string Landskode { get; set; } = "+47";

    // Selve nummeret skrives uten landskode, bare tall (mellomrom er greit).
    [RegularExpression(@"^[0-9 ]{5,15}$",
        ErrorMessage = "Skriv telefonnummeret uten landskode, bare tall.")]
    public string? KontaktTelefon { get; set; }

    [EmailAddress(ErrorMessage = "Ugyldig e-postadresse.")]
    public string? KontaktEpost { get; set; }

    // Regler som gjelder flere felt sammen. ASP.NET kjører denne etter
    // reglene over ([Required] osv.) og viser feilmeldingene ved feltene.
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        var feil = new List<ValidationResult>();

        if (Type == BehovType.Annet && string.IsNullOrWhiteSpace(AnnetType))
        {
            feil.Add(new ValidationResult("Oppgi hva slags behov det er.",
                new[] { nameof(AnnetType) }));
        }

        if (string.IsNullOrWhiteSpace(KontaktTelefon) && string.IsNullOrWhiteSpace(KontaktEpost))
        {
            feil.Add(new ValidationResult("Oppgi telefonnummer eller e-post.",
                new[] { nameof(KontaktTelefon), nameof(KontaktEpost) }));
        }

        if (!Land.KodeFinnes(Landskode))
        {
            feil.Add(new ValidationResult("Velg en landskode fra listen.",
                new[] { nameof(Landskode) }));
        }

        if (!string.IsNullOrWhiteSpace(KommuneNavn) && Kommuner.FinnFraTekst(KommuneNavn) == null)
        {
            feil.Add(new ValidationResult("Fant ikke kommunen. Velg en kommune fra listen.",
                new[] { nameof(KommuneNavn) }));
        }

        return feil;
    }

    // Kopierer feltene fra skjemaet over til behovet i databasen.
    // Brukes både når et behov meldes og når det endres. Status, eier og
    // tidspunkt settes i controlleren, aldri fra skjemaet.
    public void KopierTil(Behov behov)
    {
        // Type, posisjon og prioritet er påkrevd, så de har alltid en verdi her
        // (controlleren sjekker ModelState.IsValid først).
        if (Type != null)
        {
            behov.Type = Type.Value;
        }
        if (Latitude != null)
        {
            behov.Latitude = Latitude.Value;
        }
        if (Longitude != null)
        {
            behov.Longitude = Longitude.Value;
        }
        if (Prioritet != null)
        {
            behov.Prioritet = Prioritet.Value;
        }

        behov.AnnetType = AnnetTypeForLagring();

        // Kommunen lagres som nummer. Tomt felt gir ingen kommune.
        Kommune? kommune = Kommuner.FinnFraTekst(KommuneNavn);
        if (kommune == null)
        {
            behov.Kommunenummer = null;
        }
        else
        {
            behov.Kommunenummer = kommune.Nummer;
        }
        behov.Beskrivelse = Beskrivelse;
        behov.KontaktTelefon = TelefonForLagring();
        behov.KontaktEpost = KontaktEpost;
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

    // Fritekst for «Annet» lagres bare når typen faktisk er «Annet».
    // Bytter brukeren fra «Annet» til for eksempel «Transport», forsvinner den gamle teksten.
    public string? AnnetTypeForLagring()
    {
        if (Type == BehovType.Annet && !string.IsNullOrWhiteSpace(AnnetType))
        {
            return AnnetType.Trim();
        }
        return null;
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
        foreach (Land land in Land.Alle)
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
            AnnetType = behov.AnnetType,
            Beskrivelse = behov.Beskrivelse,
            Latitude = behov.Latitude,
            Longitude = behov.Longitude,
            Prioritet = behov.Prioritet,
            KommuneNavn = Kommuner.NavnEllerTomt(behov.Kommunenummer),
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
                return Oversetter.Linjer("Hva skal fraktes (personer eller utstyr):", "Fra hvor og til hvor:", "Når trengs det:");
            case BehovType.Drone:
                return Oversetter.Linjer("Hva skal dronen brukes til (søk, oversikt, levering):", "Område:", "Når trengs det:");
            case BehovType.Generator:
                return Oversetter.Linjer("Hva trenger strøm (for eksempel medisinsk utstyr):", "Omtrent hvor mye effekt (kW), hvis du vet:", "Hvor lenge:");
            case BehovType.Evakuering:
                return Oversetter.Linjer("Antall personer:", "Trenger noen spesiell hjelp (rullestol, medisiner):", "Hvor skal de:");
            default:
                return "";
        }
    }
}
