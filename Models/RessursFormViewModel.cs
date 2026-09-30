using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.Models
{
    public class RessursFormViewModel
    {
        [Required(ErrorMessage = "Du må velge type ressurs.")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Posisjon er påkrevd.")]
        public string Posisjon { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        public DateTime? TilgjengeligFra { get; set; }

        public string? Beskrivelse { get; set; }

        public string? Navn { get; set; }

        [Phone(ErrorMessage = "Ugyldig telefonnummer.")]
        public string? Telefon { get; set; }

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

        public bool ErValidTilgjengeligFra()
        {
            return TilgjengeligFra == null || TilgjengeligFra >= DateTime.Now;
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
            return Telefon;
        }
    }
}
