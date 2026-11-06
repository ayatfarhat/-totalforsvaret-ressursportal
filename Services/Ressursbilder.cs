namespace Nabohjelp.Services;

// Alt som har med bildene til ressursene å gjøre: sjekke, lagre, finne og slette.
// Brukes av RessursController og av KontoService (når en konto slettes).
//
// Bildene ligger i wwwroot/uploads/ressurser og heter ressurs-ID + filendelse,
// for eksempel 12.jpg. Da trenger vi ingen egen kolonne i databasen for bildet.
// webRoot er mappen wwwroot (IWebHostEnvironment.WebRootPath i controlleren).
public static class Ressursbilder
{
    public const long MaksStorrelse = 5 * 1024 * 1024; // 5 MB

    private static readonly string[] TillatteEndelser = { ".jpg", ".jpeg", ".png", ".webp" };

    // Endelsene et lagret bilde kan ha (.jpeg lagres som .jpg).
    private static readonly string[] LagredeEndelser = { ".jpg", ".png", ".webp" };

    // Sjekker at filen er et bilde vi tillater.
    // Gir en feilmelding, eller null hvis bildet er i orden.
    public static string? Sjekk(IFormFile fil)
    {
        if (fil.Length > MaksStorrelse)
        {
            return "Bildet er for stort. Maks størrelse er 5 MB.";
        }

        string endelse = Path.GetExtension(fil.FileName).ToLowerInvariant();
        if (!TillatteEndelser.Contains(endelse))
        {
            return "Bildet må være JPG, PNG eller WebP.";
        }

        if (!fil.ContentType.StartsWith("image/"))
        {
            return "Filen er ikke et bilde.";
        }

        // Filnavnet og Content-Type kommer fra nettleseren og kan være falske.
        // Derfor ser vi også på de første bytene i selve filen.
        if (!ErEkteBilde(fil, endelse))
        {
            return "Filen ser ikke ut som et ekte bilde.";
        }

        return null;
    }

    // Lagrer bildet som «ressurs-ID + filendelse», for eksempel 12.jpg.
    // Vi bruker ikke filnavnet brukeren sendte, slik at ingen kan
    // overskrive andre filer eller lage rare filnavn.
    public static void Lagre(string webRoot, IFormFile fil, int ressursId)
    {
        string endelse = Path.GetExtension(fil.FileName).ToLowerInvariant();
        if (endelse == ".jpeg")
        {
            endelse = ".jpg";
        }

        string mappe = Mappe(webRoot);
        Directory.CreateDirectory(mappe);

        string fullSti = Path.Combine(mappe, ressursId + endelse);
        using (FileStream strom = new FileStream(fullSti, FileMode.Create))
        {
            fil.CopyTo(strom);
        }
    }

    // Gir nettadressen til bildet (for eksempel /uploads/ressurser/12.jpg),
    // eller null hvis ressursen ikke har bilde.
    public static string? Finn(string webRoot, int ressursId)
    {
        foreach (string endelse in LagredeEndelser)
        {
            if (File.Exists(Path.Combine(Mappe(webRoot), ressursId + endelse)))
            {
                return "/uploads/ressurser/" + ressursId + endelse;
            }
        }

        return null;
    }

    // Sletter bildet til en ressurs, hvis det finnes.
    public static void Slett(string webRoot, int ressursId)
    {
        foreach (string endelse in LagredeEndelser)
        {
            string sti = Path.Combine(Mappe(webRoot), ressursId + endelse);
            if (File.Exists(sti))
            {
                File.Delete(sti);
            }
        }
    }

    // Mappen der bildene ligger: wwwroot/uploads/ressurser
    private static string Mappe(string webRoot)
    {
        return Path.Combine(webRoot, "uploads", "ressurser");
    }

    // Sjekker «signaturen» (de første bytene) i filen:
    //   JPEG begynner med FF D8 FF, PNG med 89 50 4E 47,
    //   WebP med «RIFF» og så «WEBP» fra byte 8.
    private static bool ErEkteBilde(IFormFile fil, string endelse)
    {
        byte[] start = new byte[12];
        int lest;
        // «using» lukker filen igjen når blokken er ferdig.
        using (Stream strom = fil.OpenReadStream())
        {
            lest = strom.Read(start, 0, start.Length);
        }

        if (lest < 12)
        {
            return false;
        }

        if (endelse == ".jpg" || endelse == ".jpeg")
        {
            return start[0] == 0xFF && start[1] == 0xD8 && start[2] == 0xFF;
        }

        if (endelse == ".png")
        {
            return start[0] == 0x89 && start[1] == 0x50 && start[2] == 0x4E && start[3] == 0x47;
        }

        if (endelse == ".webp")
        {
            return start[0] == 'R' && start[1] == 'I' && start[2] == 'F' && start[3] == 'F'
                && start[8] == 'W' && start[9] == 'E' && start[10] == 'B' && start[11] == 'P';
        }

        return false;
    }
}
