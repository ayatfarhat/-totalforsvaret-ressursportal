using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Controllers;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Xunit;

namespace Nabohjelp.Tests;

// Tester tildelingen mot en ekte (midlertidig) database: samme ressurs kan ikke
// tildeles to behov. De andre testene trenger ikke database.
public class TildelingController_Tests
{
    [Fact]
    public async Task SammeRessurs_KanIkkeTildelesToForskjelligeBehov()
    {
        // Oppretter en midlertidig SQLite-database.
        await using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);

        await db.Database.EnsureCreatedAsync();

        // Oppretter en testbruker.
        var bruker = new IdentityUser
        {
            UserName = "testbruker",
            Email = "test@example.com"
        };

        db.Users.Add(bruker);

        // Oppretter to behov som begge trenger aggregat.
        var behov1 = new Behov
        {
            Type = BehovType.Generator,
            Latitude = 63.4305,
            Longitude = 10.3951,
            Status = BehovStatus.New
        };

        var behov2 = new Behov
        {
            Type = BehovType.Generator,
            Latitude = 63.4200,
            Longitude = 10.4000,
            Status = BehovStatus.New
        };

        db.Behovsliste.AddRange(behov1, behov2);

        // Oppretter én tilgjengelig ressurs.
        var ressurs = new Ressurs
        {
            Type = "Aggregat",
            Posisjon = "Trondheim",
            Status = "Tilgjengelig",
            Breddegrad = 63.4305m,
            Lengdegrad = 10.3951m
        };

        db.Ressurser.Add(ressurs);

        await db.SaveChangesAsync();

        // Oppretter en innlogget bruker for controlleren.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, bruker.Id),
            new Claim(ClaimTypes.Role, "Kommune")
        };

        var identity = new ClaimsIdentity(
            claims,
            "TestAuthentication");

        var controller = new TildelingController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            },

            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(),
                new TestTempDataProvider())
        };

        // Første tildeling skal lykkes.
        var resultat1 = await controller.Tildel(
            behov1.Id,
            ressurs.Id);

        Assert.IsType<RedirectToActionResult>(resultat1);

        // Kontrollerer at ressursen er tildelt.
        await db.Entry(ressurs).ReloadAsync();

        Assert.Equal("Tildelt", ressurs.Status);

        // Forsøker å tildele samme ressurs til et annet behov.
        var resultat2 = await controller.Tildel(
            behov2.Id,
            ressurs.Id);

        Assert.IsType<RedirectToActionResult>(resultat2);

        // Det skal fortsatt bare finnes én tildeling.
        var antallTildelinger = await db.Tildelinger
            .CountAsync(t => t.RessursId == ressurs.Id);

        Assert.Equal(1, antallTildelinger);

        // Første behov skal være tildelt.
        await db.Entry(behov1).ReloadAsync();

        Assert.Equal(BehovStatus.Assigned, behov1.Status);

        // Andre behov skal fortsatt være nytt.
        await db.Entry(behov2).ReloadAsync();

        Assert.Equal(BehovStatus.New, behov2.Status);
    }

    [Fact]
    public async Task AngretTildeling_GjorRessursenLedigOgSkrivesIHistorikken()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var bruker = new IdentityUser { UserName = "kommune", Email = "kommune@example.com" };
        db.Users.Add(bruker);

        var behov = new Behov
        {
            Type = BehovType.Generator,
            Latitude = 58.34,
            Longitude = 8.59,
            Status = BehovStatus.UnderReview
        };
        db.Behovsliste.Add(behov);

        var ressurs = new Ressurs
        {
            Type = "Aggregat",
            Posisjon = "Grimstad",
            Status = "Tilgjengelig",
            Breddegrad = 58.35m,
            Lengdegrad = 8.60m
        };
        db.Ressurser.Add(ressurs);

        await db.SaveChangesAsync();

        var controller = LagController(db, bruker.Id);

        // Tildeler aggregatet. Ressursen settes til «Tildelt» direkte i databasen,
        // så vi leser den inn på nytt (ReloadAsync) før neste steg.
        await controller.Tildel(behov.Id, ressurs.Id);
        await db.Entry(ressurs).ReloadAsync();
        Assert.Equal("Tildelt", ressurs.Status);

        Tildeling tildeling = await db.Tildelinger.SingleAsync();
        Assert.False(tildeling.EierHarSett);

        // Angrer tildelingen.
        var resultat = await controller.Angre(tildeling.Id, null);
        Assert.IsType<RedirectToActionResult>(resultat);

        await db.Entry(ressurs).ReloadAsync();
        await db.Entry(behov).ReloadAsync();

        Assert.Equal("Tilgjengelig", ressurs.Status);
        Assert.Equal(BehovStatus.UnderReview, behov.Status);
        Assert.Equal(0, await db.Tildelinger.CountAsync());

        // Historikken har to linjer: tildelt, og så angret.
        List<StatusEndring> historikk = await db.StatusEndringer.OrderBy(s => s.Id).ToListAsync();
        Assert.Equal(2, historikk.Count);
        Assert.Equal(BehovStatus.Assigned, historikk[0].TilStatus);
        Assert.False(historikk[0].ErAngret);
        Assert.True(historikk[1].ErAngret);
        Assert.Equal("Aggregat", historikk[1].RessursType);
    }

    // Lager en TildelingController med en innlogget bruker med rollen Kommune.
    private static TildelingController LagController(ApplicationDbContext db, string brukerId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, brukerId),
            new Claim(ClaimTypes.Role, "Kommune")
        };

        return new TildelingController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication"))
                }
            },
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(),
                new TestTempDataProvider())
        };
    }
}
public class TestTempDataProvider :
    Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        return new Dictionary<string, object>();
    }

    public void SaveTempData(
        HttpContext context,
        IDictionary<string, object> values)
    {
    }
}