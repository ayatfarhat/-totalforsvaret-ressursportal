using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.Models
{
    // En ressurs som kommunen eller kriseledelsen ønsker at innbyggere registrerer.
    public class OnsketRessurs
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Du må velge type ressurs.")]
        public string Type { get; set; } = string.Empty;

        [Range(1, 999, ErrorMessage = "Antall må være mellom 1 og 999.")]
        public int Antall { get; set; } = 1;

        [Required(ErrorMessage = "Du må oppgi hvor ressursen trengs.")]
        [StringLength(100, ErrorMessage = "Stedet kan ikke være lengre enn 100 tegn.")]
        public string Sted { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Beskrivelsen kan ikke være lengre enn 300 tegn.")]
        public string? Beskrivelse { get; set; }

        // Settes av controlleren, ikke av bruker
        public DateTime Opprettet { get; set; } = DateTime.Now;

        public string VisBeskrivelse()
        {
            if (string.IsNullOrWhiteSpace(Beskrivelse))
            {
                return "Ingen";
            }
            return Beskrivelse;
        }
    }
}
