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
            // r => ... betyr «for hver ressurs r» (spørringen kjøres i databasen).
            List<Ressurs> ressurser = await _db.Ressurser
                .AsNoTracking()
                .Where(r => r.Breddegrad != null && r.Lengdegrad != null)
                .ToListAsync();

            foreach (Ressurs ressurs in ressurser)
            {
                modell.Ressurser.Add(LagRessursPunkt(ressurs));
            }

            // Behov: avsluttede behov (løst, avvist, trukket tilbake) vises ikke,
            // så kartet bare viser det som fortsatt trenger hjelp.
            List<Behov> behovsliste = await _db.Behovsliste
                .AsNoTracking()
                .Where(b => b.Status != BehovStatus.Resolved &&
                            b.Status != BehovStatus.Rejected &&
                            b.Status != BehovStatus.Withdrawn)
                .ToListAsync();

            foreach (Behov behov in behovsliste)
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
                Slag = T["Aktivt behov"],
                Tittel = Visningstekst.Type(behov.Type, behov.AnnetType),
                Breddegrad = behov.Latitude,
                Lengdegrad = behov.Longitude,
                Farge = "planlagt",
                Undertekst = T["Meldt {0}", behov.OpprettetTid.ToLocalTime().ToString("dd.MM.yyyy HH:mm")],
                Beskrivelse = Forkort(behov.Beskrivelse)
            };

            string prioritetFarge = "merke-lys";
            if (behov.Prioritet == BehovPrioritet.Urgent)
            {
                punkt.Farge = "hjelp";
                prioritetFarge = "merke-hjelp";
            }

            punkt.Merker.Add(new KartMerke { Tekst = Visningstekst.Prioritet(behov.Prioritet), Farge = prioritetFarge });
            punkt.Merker.Add(new KartMerke { Tekst = Visningstekst.Status(behov.Status), Farge = Visningstekst.StatusFarge(behov.Status) });

            LeggTilKontakt(punkt, behov.KontaktTelefon, behov.KontaktEpost);

            // Nærmeste ledige ressurs som passer, i luftlinje (se Services/Ressursmatching.cs).
            NaermesteRessurs? naermeste = Ressursmatching.FinnNaermeste(behov, ressurser);
            if (naermeste != null)
            {
                punkt.Fremhevet = T["Nærmeste ressurs: {0}, {1} km unna",
                    T[naermeste.Ressurs.Type], naermeste.AvstandKm.ToString("0.0")];
                punkt.FremhevetErFunnet = true;
            }
            else
            {
                punkt.Fremhevet = T["Ingen ledig ressurs som passer"];
            }

            punkt.Lenker.Add(new KartLenke
            {
                Tekst = T["Finn ressurs"],
                Adresse = "/Tildeling/Match/" + behov.Id
            });

            if (BehovStatusflyt.KanRedigeres(behov.Status))
            {
                punkt.Lenker.Add(new KartLenke
                {
                    Tekst = T["Endre"],
                    Adresse = "/Behov/Rediger/" + behov.Id
                });
            }

            return punkt;
        }

        // Lager prikken for en ressurs: grønn hvis tilgjengelig, grå ellers.
        private KartPunkt LagRessursPunkt(Ressurs ressurs)
        {
            var punkt = new KartPunkt
            {
                Slag = T["Ressurs"],
                Tittel = T[ressurs.Type],
                Farge = "opptatt",
                Undertekst = Kommuner.VisNavn(ressurs.Kommunenummer) + " · " + ressurs.Posisjon,
                Beskrivelse = Forkort(ressurs.Beskrivelse)
            };

            // Bare ressurser med posisjon kommer hit (se Index), så verdiene finnes.
            // decimal (slik databasen lagrer dem) gjøres om til double, som kartet bruker.
            if (ressurs.Breddegrad != null && ressurs.Lengdegrad != null)
            {
                punkt.Breddegrad = (double)ressurs.Breddegrad.Value;
                punkt.Lengdegrad = (double)ressurs.Lengdegrad.Value;
            }

            if (ressurs.Status == "Tilgjengelig")
            {
                punkt.Farge = "gi";
            }

            punkt.Merker.Add(new KartMerke { Tekst = T[ressurs.Status], Farge = Visningstekst.RessursStatusFarge(ressurs.Status) });

            if (ressurs.TilgjengeligFra != null)
            {
                punkt.LeggTilLinje(T["Fra"], ressurs.TilgjengeligFra.Value.ToString("dd.MM.yyyy HH:mm"));
            }
            punkt.LeggTilLinje(T["Navn"], ressurs.Navn);
            LeggTilKontakt(punkt, ressurs.Telefon, null);

            punkt.Lenker.Add(new KartLenke
            {
                Tekst = T["Endre"],
                Adresse = "/Ressurs/Edit/" + ressurs.Id
            });

            return punkt;
        }

        // Telefon og e-post blir lenker, så man kan ringe eller skrive rett fra kartet.
        private void LeggTilKontakt(KartPunkt punkt, string? telefon, string? epost)
        {
            if (!string.IsNullOrWhiteSpace(telefon))
            {
                // tel:-lenker skal ikke ha mellomrom.
                punkt.LeggTilLinje(T["Telefon"], telefon, "tel:" + telefon.Replace(" ", ""));
            }
            if (!string.IsNullOrWhiteSpace(epost))
            {
                punkt.LeggTilLinje(T["E-post"], epost, "mailto:" + epost);
            }
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
