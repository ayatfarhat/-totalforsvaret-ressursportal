using Microsoft.AspNetCore.Http;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for bildene til ressursene (Services/Ressursbilder.cs)
// og for at skjemaet kopieres riktig til og fra databasen (RessursFormViewModel).
public class RessursbilderTests
{
    // De første bytene i en ekte JPEG-fil, fylt opp til 12 byte.
    private static readonly byte[] JpegStart = { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 };

    // Lager en opplastet fil slik controlleren får den fra skjemaet.
    private static IFormFile LagFil(byte[] innhold, string filnavn, string contentType)
    {
        var strom = new MemoryStream(innhold);
        return new FormFile(strom, 0, innhold.Length, "bildefil", filnavn)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    // ===============================
    // ----- Sjekk av opplasting -----
    // ===============================
    [Fact]
    public void Sjekk_EkteJpg_ErGodkjent()
    {
        var fil = LagFil(JpegStart, "aggregat.jpg", "image/jpeg");

        Assert.Null(Ressursbilder.Sjekk(fil));
    }

    [Fact]
    public void Sjekk_TekstfilMedJpgNavn_BlirAvvist()
    {
        byte[] tekst = System.Text.Encoding.UTF8.GetBytes("dette er ikke et bilde");
        var fil = LagFil(tekst, "falsk.jpg", "image/jpeg");

        Assert.Equal("Filen ser ikke ut som et ekte bilde.", Ressursbilder.Sjekk(fil));
    }

    [Fact]
    public void Sjekk_GifFil_BlirAvvist()
    {
        var fil = LagFil(JpegStart, "bilde.gif", "image/gif");

        Assert.Equal("Bildet må være JPG, PNG eller WebP.", Ressursbilder.Sjekk(fil));
    }

    // ===================================
    // ----- Lagre, finne og slette -----
    // ===================================
    [Fact]
    public void LagreFinnSlett_JpegLagresSomJpg()
    {
        // En tom mappe som spiller rollen som wwwroot.
        string webRoot = Path.Combine(Path.GetTempPath(), "ressursbilder-test-" + Guid.NewGuid());

        try
        {
            Ressursbilder.Lagre(webRoot, LagFil(JpegStart, "Bilde.JPEG", "image/jpeg"), 12);
            Assert.Equal("/uploads/ressurser/12.jpg", Ressursbilder.Finn(webRoot, 12));

            Ressursbilder.Slett(webRoot, 12);
            Assert.Null(Ressursbilder.Finn(webRoot, 12));
        }
        finally
        {
            if (Directory.Exists(webRoot))
            {
                Directory.Delete(webRoot, true);
            }
        }
    }

    // ===========================================
    // ----- Skjema til og fra databasen -----
    // ===========================================
    [Fact]
    public void FraRessurs_DelerOppTelefonOgTarMedBilde()
    {
        var ressurs = new Ressurs
        {
            Id = 7,
            Type = "Aggregat",
            Posisjon = "Grimstad",
            Kommunenummer = "4202",
            Telefon = "+46 70 123 45 67",
            Status = "Tildelt",
            OpprettetAvBrukerId = "eier-1"
        };

        var skjema = RessursFormViewModel.FraRessurs(ressurs, "/uploads/ressurser/7.jpg");

        Assert.Equal(7, skjema.Id);
        Assert.Equal("+46", skjema.Landskode);
        Assert.Equal("70 123 45 67", skjema.Telefon);
        Assert.Equal("Tildelt", skjema.Status);
        Assert.Equal("/uploads/ressurser/7.jpg", skjema.BildeSti);
    }

    [Fact]
    public void KopierTil_EndrerIkkeStatusEierEllerId()
    {
        var ressurs = new Ressurs
        {
            Id = 7,
            Status = "Tildelt",
            OpprettetAvBrukerId = "eier-1"
        };

        // Selv om noen prøver å sende inn en annen status, eier og ID i skjemaet ...
        var skjema = new RessursFormViewModel
        {
            Id = 99,
            Status = "Tilgjengelig",
            OpprettetAvBrukerId = "noen-andre",
            Type = "Båt",
            Posisjon = "Lillesand",
            Landskode = "+47",
            Telefon = "12345678"
        };

        skjema.KopierTil(ressurs);

        // ... blir bare feltene brukeren kan fylle ut, kopiert.
        Assert.Equal("Båt", ressurs.Type);
        Assert.Equal("Lillesand", ressurs.Posisjon);
        Assert.Equal("+47 12345678", ressurs.Telefon);
        Assert.Equal(7, ressurs.Id);
        Assert.Equal("Tildelt", ressurs.Status);
        Assert.Equal("eier-1", ressurs.OpprettetAvBrukerId);
    }
}
