using System.ComponentModel.DataAnnotations;
using Nabohjelp.Models;
using Nabohjelp.Services;

namespace Nabohjelp.ViewModels
{
    public class RessursFormViewModel
    {
        [Required(ErrorMessage = "Du må velge type ressurs.")]
        public string Type { get; set; } = string.Empty;

        // Fylles ut når typen er «Annet»: hva slags ressurs det er.
        [StringLength(100, ErrorMessage = "Teksten kan ikke være lengre enn 100 tegn.")]
        public string? AnnetType { get; set; }

        [Required(ErrorMessage = "Posisjon er påkrevd.")]
        [StringLength(200, ErrorMessage = "Posisjonen kan ikke være lengre enn 200 tegn.")]
        public string Posisjon { get; set; } = string.Empty;

        // Kommunen ressursen står i. Brukes til å skrive ut ressurslister per region.
        [Required(ErrorMessage = "Velg kommunen ressursen står i.")]
        public string? Kommunenummer { get; set; }

        // Nøyaktige koordinater fra kartet på Create-siden.
        // Valgfritt — Posisjon (adressen) over er nok til å sende inn skjemaet.
        [Range(-90.0, 90.0, ErrorMessage = "Breddegrad må være mellom -90 og 90.")]
        public decimal? Breddegrad { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Lengdegrad må være mellom -180 og 180.")]
        public decimal? Lengdegrad { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? TilgjengeligFra { get; set; }

        [StringLength(1000, ErrorMessage = "Beskrivelsen kan ikke være lengre enn 1000 tegn.")]
        public string? Beskrivelse { get; set; }

        [StringLength(100, ErrorMessage = "Navnet kan ikke være lengre enn 100 tegn.")]
        public string? Navn { get; set; }

        public int Id { get; set; }

        public string? OpprettetAvBrukerId { get; set; }

        // Landskoden velges i en egen nedtrekksliste (se Models/Land.cs). Standard er +47.
        public string Landskode { get; set; } = "+47";

        // Selve nummeret skrives uten landskode, bare tall (mellomrom er greit).
        [RegularExpression(@"^[0-9 ]{5,15}$",
            ErrorMessage = "Skriv telefonnummeret uten landskode, bare tall.")]
        public string? Telefon { get; set; }

        // Settes av controlleren hvis brukeren lastet opp et bilde.
        // Inneholder nettadressen til bildet, for eksempel /uploads/ressurser/abc.jpg
        public string? BildeSti { get; set; }

        // Krysses av i redigeringsskjemaet hvis eieren vil fjerne bildet.
        public bool FjernBilde { get; set; }

        // Settes automatisk av controlleren ved oppretting, ikke av bruker
        public string Status { get; set; } = "Ny";

        // Fast liste over ressurstyper — brukes i dropdown-menyen
        public static readonly List<string> Typer = new List<string>
        {
            "Drone og droneoperatør",
            "ATV",
            "Traktor",
            "Gravemaskin",
            "Hjullaster",
            "Skogsmaskin",
            "Snøskuter",
            "Lastebil",
            "Sand/grus",
            "Aggregat",
            "Lokaler",
            "Sambandsutstyr",
            "Båt",
            "Mannskap/frivillige",
            "Annet"
        };

        // Forslag til beskrivelse som fylles ut automatisk når brukeren velger type.
        // Brukeren kan fritt endre teksten etterpå. Hver linje oversettes for seg.
        public static string HentForslag(string type)
        {
            switch (type)
            {
                case "Drone og droneoperatør":
                    return Oversetter.Linjer("Modell:", "Rekkevidde og flytid:", "Droneførerbevis (ja/nei):");
                case "Mannskap/frivillige":
                    return Oversetter.Linjer("Ferdigheter:", "Antall personer:", "Hvor mange timer kan dere bidra:");
                case "Sand/grus":
                    return Oversetter.Linjer("Mengde (m3 eller tonn):", "Hvordan kan det hentes:");
                case "Lokaler":
                    return Oversetter.Linjer("Antall plasser:", "Strøm og vann (ja/nei):", "Tilgang for rullestol (ja/nei):");
                case "Aggregat":
                    return Oversetter.Linjer("Effekt (kW):", "Drivstoff:", "Følger det med kabler (ja/nei):");
                case "Sambandsutstyr":
                    return Oversetter.Linjer("Type utstyr:", "Rekkevidde:", "Antall:");
                case "Båt":
                    return Oversetter.Linjer("Type og lengde:", "Antall personer den tar:", "Har du båtførerbevis (ja/nei):");
                case "Annet":
                    return "";
                default:
                    // Gjelder kjøretøy og maskiner (ATV, traktor, lastebil osv.)
                    return Oversetter.Linjer("Modell og størrelse:", "Tilstand:", "Kan du kjøre den selv (ja/nei):");
            }
        }

        // Fritekst for «Annet» lagres bare når typen faktisk er «Annet».
        // Brukes også av ønskede ressurser (OnsketRessursController).
        public static string? AnnetTypeForLagring(string type, string? annetType)
        {
            if (type == "Annet" && !string.IsNullOrWhiteSpace(annetType))
            {
                return annetType.Trim();
            }
            return null;
        }

        // Typen slik den vises i lister, for eksempel «Annet: Vannpumpe».
        public string VisType()
        {
            return Visningstekst.RessursType(Type, AnnetType);
        }

        public bool ErValidTilgjengeligFra()
        {
            // Skjemaet (datetime-local) sender tidspunktet uten sekunder. Hvis noen
            // velger «nå», er tidspunktet allerede noen sekunder gammelt når skjemaet
            // sendes inn. Derfor tillater vi ett minutt «slingringsmonn».
            return TilgjengeligFra == null || TilgjengeligFra >= DateTime.Now.AddMinutes(-1);
        }

        // Disse metodene lager tekst som er enkel å vise i sidene,
        // slik at sidene slipper egne if-setninger for tomme felt.
        public string VisTilgjengeligFra()
        {
            if (TilgjengeligFra == null)
            {
                return Oversetter.Tekst("Ikke oppgitt");
            }
            return TilgjengeligFra.Value.ToString("dd.MM.yyyy HH:mm");
        }

        public string VisBeskrivelse()
        {
            if (string.IsNullOrWhiteSpace(Beskrivelse))
            {
                return Oversetter.Tekst("Ingen");
            }
            return Beskrivelse;
        }

        public string VisNavn()
        {
            if (string.IsNullOrWhiteSpace(Navn))
            {
                return Oversetter.Tekst("Ikke oppgitt");
            }
            return Navn;
        }

        public string VisTelefon()
        {
            if (string.IsNullOrWhiteSpace(Telefon))
            {
                return Oversetter.Tekst("Ikke oppgitt");
            }
            if (string.IsNullOrEmpty(Landskode))
            {
                return Telefon;
            }
            return Landskode + " " + Telefon;
        }

        // Brukes i tel:-lenker, uten mellomrom.
        public string TelefonLenke()
        {
            if (string.IsNullOrWhiteSpace(Telefon))
            {
                return "";
            }
            return Landskode + Telefon.Replace(" ", "");
        }

        // I databasen lagres landskode og nummer sammen, for eksempel «+47 12345678».
        public string? TelefonForLagring()
        {
            if (string.IsNullOrWhiteSpace(Telefon))
            {
                return null;
            }
            return Landskode + " " + Telefon.Trim();
        }

        // Deler et lagret nummer («+47 12345678») opp i landskode og nummer igjen.
        public void LesTelefonFraLagring(string? lagret)
        {
            if (string.IsNullOrWhiteSpace(lagret))
            {
                Telefon = null;
                return;
            }

            lagret = lagret.Trim();
            foreach (Land land in Land.Alle)
            {
                if (lagret.StartsWith(land.Kode + " "))
                {
                    Landskode = land.Kode;
                    Telefon = lagret.Substring(land.Kode.Length + 1).Trim();
                    return;
                }
            }

            // Ressurser som ble lagret før landskoden fantes.
            if (lagret.StartsWith("+"))
            {
                Landskode = "";
                Telefon = lagret;
                return;
            }

            Landskode = "+47";
            Telefon = lagret;
        }

        // Lager skjemaet fra en ressurs i databasen (til visning og redigering).
        // bildeSti finner controlleren med Ressursbilder.Finn, siden bildet ligger på disken.
        public static RessursFormViewModel FraRessurs(Ressurs ressurs, string? bildeSti)
        {
            var skjema = new RessursFormViewModel
            {
                Id = ressurs.Id,
                OpprettetAvBrukerId = ressurs.OpprettetAvBrukerId,
                Type = ressurs.Type,
                AnnetType = ressurs.AnnetType,
                Posisjon = ressurs.Posisjon,
                Kommunenummer = ressurs.Kommunenummer,
                Breddegrad = ressurs.Breddegrad,
                Lengdegrad = ressurs.Lengdegrad,
                TilgjengeligFra = ressurs.TilgjengeligFra,
                Beskrivelse = ressurs.Beskrivelse,
                Navn = ressurs.Navn,
                Status = ressurs.Status,
                BildeSti = bildeSti
            };

            // Telefonen er lagret som «+47 12345678». Vi deler den opp igjen
            // i landskode og nummer, slik at skjemaet viser dem hver for seg.
            skjema.LesTelefonFraLagring(ressurs.Telefon);

            return skjema;
        }

        // Kopierer feltene brukeren kan fylle ut, over til ressursen i databasen.
        // Brukes både ved registrering og redigering. Status, eier og ID settes
        // aldri herfra, så brukeren kan ikke endre dem gjennom skjemaet.
        public void KopierTil(Ressurs ressurs)
        {
            ressurs.Type = Type;
            ressurs.AnnetType = AnnetTypeForLagring(Type, AnnetType);
            ressurs.Posisjon = Posisjon;
            ressurs.Kommunenummer = Kommunenummer;
            ressurs.Breddegrad = Breddegrad;
            ressurs.Lengdegrad = Lengdegrad;
            ressurs.TilgjengeligFra = TilgjengeligFra;
            ressurs.Beskrivelse = Beskrivelse;
            ressurs.Navn = Navn;
            ressurs.Telefon = TelefonForLagring();
        }

        public string VisKommune()
        {
            return Kommuner.VisNavn(Kommunenummer);
        }

        public string VisKoordinater()
        {
            if (Breddegrad == null || Lengdegrad == null)
            {
                return Oversetter.Tekst("Ikke valgt på kart");
            }
            // Viser opptil fem desimaler, for eksempel «58.34051, 8.59342».
            return Breddegrad.Value.ToString("0.#####") + ", " + Lengdegrad.Value.ToString("0.#####");
        }
    }
}
