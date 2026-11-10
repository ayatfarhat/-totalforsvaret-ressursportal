using Microsoft.AspNetCore.Http;
using Nabohjelp.Models;
using Nabohjelp.Services;
using Xunit;

namespace Nabohjelp.Tests;

// Tester for søket på nettsiden (Services/Nettstedsok.cs)
// og for beskyttelsen mot søppelpost (Sikkerhet.ErRobot).
public class SokTests
{
    [Theory]
    [InlineData("aggregat", true)]
    [InlineData("AGGREGAT grimstad", true)]   // store bokstaver og flere ord
    [InlineData("aggregat arendal", false)]   // alle ordene må finnes
    [InlineData("   ", false)]                // tomt søk gir ingen treff
    public void Passer_AlleOrdeneMaaFinnes(string sok, bool forventet)
    {
        Assert.Equal(forventet, Nettstedsok.Passer(sok, new List<string?> { "Aggregat", null, "Grimstad sentrum" }));
    }

    [Fact]
    public void FinnSider_ViserBareSiderBrukerenHarTilgangTil()
    {
        Assert.Empty(Nettstedsok.FinnSider("invitasjoner", erAnsatt: true, erAdmin: false));
        Assert.Single(Nettstedsok.FinnSider("invitasjoner", erAnsatt: true, erAdmin: true));
        Assert.Single(Nettstedsok.FinnSider("cookies", erAnsatt: false, erAdmin: false));
    }

    [Fact]
    public void FinnRessurserOgSporsmal_SokerIAnnetTekstOgSvar()
    {
        var ressurser = new List<Ressurs>
        {
            new Ressurs { Id = 4, Type = "Annet", AnnetType = "Vannpumpe", Posisjon = "Storgata 1" },
            new Ressurs { Id = 5, Type = "Båt", Posisjon = "Brygga" }
        };

        List<ViewModels.SokTreff> treff = Nettstedsok.FinnRessurser("vannpumpe", ressurser);

        Assert.Single(treff);
        Assert.Equal("/Ressurs/Confirmation/4", treff[0].Adresse);
        Assert.Contains(Nettstedsok.FinnSporsmal("tofaktor"), t => t.Adresse.StartsWith("/Home/Sporsmal#sporsmal-"));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("https://reklame.example", true)]
    public void ErRobot_NaarDetSkjulteFeltetErFyltUt(string verdi, bool forventet)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            [Sikkerhet.HonningkrukkeFelt] = verdi
        });

        Assert.Equal(forventet, Sikkerhet.ErRobot(context.Request));
    }
}
