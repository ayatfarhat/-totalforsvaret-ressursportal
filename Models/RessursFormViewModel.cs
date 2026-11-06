using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.Models
{
    public class RessursFormViewModel
    {
        [Required(ErrorMessage = "Du må velge type ressurs.")]
        public string Type { get; set; } = string.Empty;

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

        // Landskoden velges i en egen nedtrekksliste (standard er +47).
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
        public static readonly List<string> Typer = new()
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

        // Landskoder som kan velges. Første i listen er standard.
        public static readonly List<(string Kode, string Land)> Landskoder = new()
        {
            ("+47", "Norge"),
            ("+46", "Sverige"),
            ("+45", "Danmark"),
            ("+358", "Finland"),
            ("+354", "Island"),
            ("+44", "Storbritannia"),
            ("+49", "Tyskland"),
            ("+48", "Polen")
        };

        // Forslag til beskrivelse som fylles ut automatisk når brukeren velger type.
        // Brukeren kan fritt endre teksten etterpå.
        public static string HentForslag(string type)
        {
            switch (type)
            {
                case "Drone og droneoperatør":
                    return "Modell:\nRekkevidde og flytid:\nDroneførerbevis (ja/nei):";
                case "Mannskap/frivillige":
                    return "Ferdigheter:\nAntall personer:\nHvor mange timer kan dere bidra:";
                case "Sand/grus":
                    return "Mengde (m3 eller tonn):\nHvordan kan det hentes:";
                case "Lokaler":
                    return "Antall plasser:\nStrøm og vann (ja/nei):\nTilgang for rullestol (ja/nei):";
                case "Aggregat":
                    return "Effekt (kW):\nDrivstoff:\nFølger det med kabler (ja/nei):";
                case "Sambandsutstyr":
                    return "Type utstyr:\nRekkevidde:\nAntall:";
                case "Båt":
                    return "Type og lengde:\nAntall personer den tar:\nHar du båtførerbevis (ja/nei):";
                case "Annet":
                    return "";
                default:
                    // Gjelder kjøretøy og maskiner (ATV, traktor, lastebil osv.)
                    return "Modell og størrelse:\nTilstand:\nKan du kjøre den selv (ja/nei):";
            }
        }

        public bool ErValidTilgjengeligFra()
        {
            // Skjemaet (datetime-local) sender tidspunktet uten sekunder. Hvis noen
            // velger «nå», er tidspunktet allerede noen sekunder gammelt når skjemaet
            // sendes inn. Derfor tillater vi ett minutt «slingringsmonn».
            return TilgjengeligFra == null || TilgjengeligFra >= DateTime.Now.AddMinutes(-1);
        }

        // Disse metodene lager tekst som er enkel å vise i View-ene,
        // slik at View-ene slipper ?./??/ternary-uttrykk.
        public string VisTilgjengeligFra()
        {
            if (TilgjengeligFra == null)
            {
                return "Ikke oppgitt";
            }
            return TilgjengeligFra.Value.ToString("dd.MM.yyyy HH:mm");
        }

        public string VisBeskrivelse()
        {
            if (string.IsNullOrWhiteSpace(Beskrivelse))
            {
                return "Ingen";
            }
            return Beskrivelse;
        }

        public string VisNavn()
        {
            if (string.IsNullOrWhiteSpace(Navn))
            {
                return "Ikke oppgitt";
            }
            return Navn;
        }

        public string VisTelefon()
        {
            if (string.IsNullOrWhiteSpace(Telefon))
            {
                return "Ikke oppgitt";
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
            foreach (var land in Landskoder)
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
                return "Ikke valgt på kart";
            }
            return $"{Breddegrad:0.#####}, {Lengdegrad:0.#####}";
        }
    }
}
