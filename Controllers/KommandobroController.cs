using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.Services;
using Nabohjelp.ViewModels;

namespace Nabohjelp.Controllers
{
    // Kommandobro: kart og tall for kriseledelsen.
    // Bare Admin og Kommune kommer inn her (med 2FA når det kreves, se Services/Tilgang.cs).
    // Andre blir sendt til /Account/AccessDenied, og den som ikke er logget inn, til innlogging.
    [Authorize(Policy = "Ansatt")]
    public class KommandobroController : Controller
    {
        // Lengste beskrivelse som vises i boksen på kartet. Resten kuttes med «…».
        private const int MaksBeskrivelse = 200;

        private readonly ApplicationDbContext _db;
        private readonly IStringLocalizer<SharedResource> T;

        public KommandobroController(ApplicationDbContext db, IStringLocalizer<SharedResource> localizer)
        {
            _db = db;
            T = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var modell = new MapViewModel();

            // Ressurser: bare de som har en posisjon valgt på kartet, vises.
            var ressurser = await _db.Ressurser
                .AsNoTracking()
                .Where(r => r.Breddegrad != null && r.Lengdegrad != null)
                .ToListAsync();

            foreach (var ressurs in ressurser)
            {
                modell.Ressurser.Add(LagRessursPunkt(ressurs));
            }

            // Behov: avsluttede behov (løst, avvist, trukket tilbake) vises ikke,
            // så kartet bare viser det som fortsatt trenger hjelp.
            var behovsliste = await _db.Behovsliste
                .AsNoTracking()
                .Where(b => b.Status != BehovStatus.Resolved &&
                            b.Status != BehovStatus.Rejected &&
                            b.Status != BehovStatus.Withdrawn)
                .ToListAsync();

            foreach (var behov in behovsliste)
            {
                modell.Behov.Add(LagBehovPunkt(behov, ressurser));
            }

            return View(modell);
        }

        // Lager prikken for et behov: rød hvis akutt, gul hvis planlagt.
        // ressurser brukes til å finne den nærmeste ledige ressursen som passer.
        private KartPunkt LagBehovPunkt(Behov behov, List<Ressurs> ressurser)
        {
            var punkt = new KartPunkt
            {
                Tittel = T["Behov: {0}", Visningstekst.Type(behov.Type)],
                Breddegrad = behov.Latitude,
                Lengdegrad = behov.Longitude,
                Farge = behov.Prioritet == BehovPrioritet.Urgent ? "hjelp" : "planlagt"
            };

            // Når typen er «Annet», står det brukeren skrev i en egen linje.
            if (behov.Type == BehovType.Annet)
            {
                punkt.LeggTilLinje(T["Hva"], behov.AnnetType);
            }

            punkt.LeggTilLinje(T["Prioritet"], Visningstekst.Prioritet(behov.Prioritet));
            punkt.LeggTilLinje(T["Status"], Visningstekst.Status(behov.Status));
            punkt.LeggTilLinje(T["Meldt"], behov.OpprettetTid.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));
            punkt.LeggTilLinje(T["Beskrivelse"], Forkort(behov.Beskrivelse));
            punkt.LeggTilLinje(T["Telefon"], behov.KontaktTelefon);
            punkt.LeggTilLinje(T["E-post"], behov.KontaktEpost);

            // Nærmeste ledige ressurs som passer, i luftlinje (se Services/Ressursmatching.cs).
            var naermeste = Ressursmatching.FinnNaermeste(behov, ressurser);
            if (naermeste != null)
            {
                punkt.LeggTilLinje(T["Nærmeste ressurs"], T["{0}, {1} km unna",
                    T[naermeste.Ressurs.Type], naermeste.AvstandKm.ToString("0.0")]);
            }
            else
            {
                punkt.LeggTilLinje(T["Nærmeste ressurs"], T["Ingen ledig ressurs som passer"]);
            }

            punkt.Lenker.Add(new KartLenke
            {
                Tekst = T["Finn ressurs"],
                Adresse = Url.Action("Match", "Tildeling", new { id = behov.Id }) ?? ""
            });

            if (BehovStatusflyt.KanRedigeres(behov.Status))
            {
                punkt.Lenker.Add(new KartLenke
                {
                    Tekst = T["Endre"],
                    Adresse = Url.Action("Rediger", "Behov", new { id = behov.Id }) ?? ""
                });
            }

            return punkt;
        }

        // Lager prikken for en ressurs: grønn hvis tilgjengelig, grå ellers.
        private KartPunkt LagRessursPunkt(Ressurs ressurs)
        {
            var punkt = new KartPunkt
            {
                Tittel = T["Ressurs: {0}", T[ressurs.Type]],
                Breddegrad = (double)ressurs.Breddegrad!.Value,
                Lengdegrad = (double)ressurs.Lengdegrad!.Value,
                Farge = ressurs.Status == "Tilgjengelig" ? "gi" : "opptatt"
            };

            punkt.LeggTilLinje(T["Status"], T[ressurs.Status]);
            if (!string.IsNullOrEmpty(ressurs.Kommunenummer))
            {
                punkt.LeggTilLinje(T["Kommune"], Kommuner.VisNavn(ressurs.Kommunenummer));
            }
            punkt.LeggTilLinje(T["Posisjon"], ressurs.Posisjon);
            punkt.LeggTilLinje(T["Tilgjengelig fra"], ressurs.TilgjengeligFra?.ToString("dd.MM.yyyy HH:mm"));
            punkt.LeggTilLinje(T["Beskrivelse"], Forkort(ressurs.Beskrivelse));
            punkt.LeggTilLinje(T["Navn"], ressurs.Navn);
            punkt.LeggTilLinje(T["Telefon"], ressurs.Telefon);

            punkt.Lenker.Add(new KartLenke
            {
                Tekst = T["Endre"],
                Adresse = Url.Action("Edit", "Ressurs", new { id = ressurs.Id }) ?? ""
            });

            return punkt;
        }

        // Kutter lange beskrivelser, så boksen på kartet ikke blir for stor.
        private static string? Forkort(string? tekst)
        {
            if (tekst == null || tekst.Length <= MaksBeskrivelse)
            {
                return tekst;
            }
            return tekst.Substring(0, MaksBeskrivelse).TrimEnd() + "…";
        }
    }
}
