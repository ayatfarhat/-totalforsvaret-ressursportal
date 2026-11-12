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
    // Bare Admin og Kommune har tilgang til denne controlleren.
    [Authorize(Policy = "Ansatt")]
    public class KommandobroController : Controller
    {
        // Maksimal lengde på beskrivelser som vises på kartet.
        private const int MaksBeskrivelse = 200;

        private readonly ApplicationDbContext _db;
        private readonly IStringLocalizer<SharedResource> T;

        public KommandobroController(
            ApplicationDbContext db,
            IStringLocalizer<SharedResource> localizer)
        {
            _db = db;
            T = localizer;
        }

        // GET: /Kommandobro
        // Viser Kommandobroen med ressurser og aktive behov.
        public async Task<IActionResult> Index()
        {
            // Henter kartdata fra databasen.
            MapViewModel modell = await HentKartdata();

            // Sender kartdataene til nettsiden.
            return View(modell);
        }

        // GET: /Kommandobro/Kartdata
        // Henter oppdaterte kartdata uten å laste inn hele nettsiden.
        // Kan brukes av JavaScript til automatisk oppdatering av kartet.
        [HttpGet]
        public async Task<IActionResult> Kartdata()
        {
            // Henter de nyeste ressursene og behovene fra databasen.
            MapViewModel modell = await HentKartdata();

            // Returnerer kartpunktene som JSON, med samme egenskapsnavn som kart.js forventer.
            // PropertyNamingPolicy = null beholder store og små bokstaver fra C#-modellene.
            return new JsonResult(
                new
                {
                    ressurser = modell.Ressurser,
                    behov = modell.Behov
                },
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                });
        }

        // Felles metode for å hente kartdata.
        // Brukes både når Kommandobroen åpnes og når kartet oppdateres.
        // Dette hindrer at vi må skrive den samme databasekoden to ganger.
        private async Task<MapViewModel> HentKartdata()
        {
            // Oppretter modellen som skal inneholde kartpunktene.
            var modell = new MapViewModel();

            // Henter ressurser som har registrert posisjon.
            // AsNoTracking gjør lesingen mer effektiv fordi dataene ikke skal endres.
            List<Ressurs> ressurser = await _db.Ressurser
                .AsNoTracking()
                .Where(r => r.Breddegrad != null && r.Lengdegrad != null)
                .ToListAsync();

            // Gjør ressursene om til punkter som kan vises på kartet.
            foreach (Ressurs ressurs in ressurser)
            {
                modell.Ressurser.Add(LagRessursPunkt(ressurs));
            }

            // Henter behov som fortsatt er aktive.
            // Løste, avviste og tilbaketrukne behov skal ikke vises.
            List<Behov> behovsliste = await _db.Behovsliste
                .AsNoTracking()
                .Where(b => b.Status != BehovStatus.Resolved &&
                            b.Status != BehovStatus.Rejected &&
                            b.Status != BehovStatus.Withdrawn)
                .ToListAsync();

            // Gjør behovene om til kartpunkter.
            // Ressurslisten brukes til å finne nærmeste passende ressurs.
            foreach (Behov behov in behovsliste)
            {
                modell.Behov.Add(LagBehovPunkt(behov, ressurser));
            }

            // Returnerer alle kartpunktene.
            return modell;
        }

        // Lager kartpunkt for et behov.
        // Akutte behov vises med rød markør, andre med gul.
        private KartPunkt LagBehovPunkt(Behov behov, List<Ressurs> ressurser)
        {
            var punkt = new KartPunkt
            {
                Slag = T["Aktivt behov"],
                Tittel = Visningstekst.Type(behov.Type, behov.AnnetType),
                Breddegrad = behov.Latitude,
                Lengdegrad = behov.Longitude,
                Farge = "planlagt",
                Undertekst = T[
                    "Meldt {0}",
                    behov.OpprettetTid.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                ],
                Beskrivelse = Forkort(behov.Beskrivelse)
            };

            // Kommunen vises foran registreringstidspunktet når den er kjent.
            // Dette gjør det enklere å se hvor behovet er registrert.
            if (Kommuner.Finnes(behov.Kommunenummer))
            {
                punkt.Undertekst = Kommuner.VisNavn(behov.Kommunenummer)
                    + " · " + punkt.Undertekst;
            }

            // Bestemmer fargen på behovsmarkøren ut fra prioriteten.
            // Akutte behov vises med rød markør.
            string prioritetFarge = "merke-lys";

            if (behov.Prioritet == BehovPrioritet.Urgent)
            {
                punkt.Farge = "hjelp";
                prioritetFarge = "merke-hjelp";
            }

            // Legger til merker som viser prioritet og status.
            punkt.Merker.Add(new KartMerke
            {
                Tekst = Visningstekst.Prioritet(behov.Prioritet),
                Farge = prioritetFarge
            });

            punkt.Merker.Add(new KartMerke
            {
                Tekst = Visningstekst.Status(behov.Status),
                Farge = Visningstekst.StatusFarge(behov.Status)
            });

            // Kontaktinformasjon er bare tilgjengelig for autoriserte ansatte.
            LeggTilKontakt(
                punkt,
                behov.KontaktTelefon,
                behov.KontaktEpost
            );

            LeggTilNaermesteRessurs(punkt, behov, ressurser);
            LeggTilBehovLenker(punkt, behov);

            return punkt;
        }

        // Skriver i boksen hvilken ledig ressurs som passer og ligger nærmest behovet,
        // i luftlinje (se Services/Ressursmatching.cs), eller at ingen passer.
        private void LeggTilNaermesteRessurs(KartPunkt punkt, Behov behov, List<Ressurs> ressurser)
        {
            NaermesteRessurs? naermeste =
                Ressursmatching.FinnNaermeste(behov, ressurser);

            if (naermeste == null)
            {
                punkt.Fremhevet = T["Ingen ledig ressurs som passer"];
                return;
            }

            punkt.Fremhevet = T[
                "Nærmeste ressurs: {0}, {1} km unna",
                Visningstekst.RessursType(
                    naermeste.Ressurs.Type,
                    naermeste.Ressurs.AnnetType
                ),
                naermeste.AvstandKm.ToString("0.0")
            ];

            punkt.FremhevetErFunnet = true;
        }

        // Knappene nederst i boksen: «Finn ressurs», og «Endre» hvis behovet kan endres.
        private void LeggTilBehovLenker(KartPunkt punkt, Behov behov)
        {
            // Lenke til siden der behovet kan matches med ressurser.
            punkt.Lenker.Add(new KartLenke
            {
                Tekst = T["Finn ressurs"],
                Adresse = "/Tildeling/Match/" + behov.Id
            });

            // Viser redigeringslenke dersom behovets status tillater endringer.
            if (BehovStatusflyt.KanRedigeres(behov.Status))
            {
                punkt.Lenker.Add(new KartLenke
                {
                    Tekst = T["Endre"],
                    Adresse = "/Behov/Rediger/" + behov.Id
                });
            }
        }

        // Lager kartpunkt for en registrert ressurs.
        // Tilgjengelige ressurser vises grønt, andre vises grått.
        private KartPunkt LagRessursPunkt(Ressurs ressurs)
        {
            var punkt = new KartPunkt
            {
                Slag = T["Ressurs"],
                Tittel = Visningstekst.RessursType(
                    ressurs.Type,
                    ressurs.AnnetType
                ),
                Farge = "opptatt",
                Undertekst =
                    Kommuner.VisNavn(ressurs.Kommunenummer)
                    + " · "
                    + ressurs.Posisjon,
                Beskrivelse = Forkort(ressurs.Beskrivelse)
            };

            // Legger til koordinatene slik at ressursen kan vises på kartet.
            if (ressurs.Breddegrad != null &&
                ressurs.Lengdegrad != null)
            {
                punkt.Breddegrad = (double)ressurs.Breddegrad.Value;
                punkt.Lengdegrad = (double)ressurs.Lengdegrad.Value;
            }

            // Grønne markører viser tilgjengelige ressurser.
            if (ressurs.Status == "Tilgjengelig")
            {
                punkt.Farge = "gi";
            }

            // Viser ressursens status i informasjonsboksen.
            punkt.Merker.Add(new KartMerke
            {
                Tekst = T[ressurs.Status],
                Farge = Visningstekst.RessursStatusFarge(ressurs.Status)
            });

            // Viser tidspunktet ressursen er tilgjengelig fra.
            if (ressurs.TilgjengeligFra != null)
            {
                punkt.LeggTilLinje(
                    T["Fra"],
                    ressurs.TilgjengeligFra.Value.ToString("dd.MM.yyyy HH:mm")
                );
            }

            // Viser navn og kontaktinformasjon.
            punkt.LeggTilLinje(T["Navn"], ressurs.Navn);

            LeggTilKontakt(
                punkt,
                ressurs.Telefon,
                null
            );

            // Lenke til redigering av ressursen.
            punkt.Lenker.Add(new KartLenke
            {
                Tekst = T["Endre"],
                Adresse = "/Ressurs/Edit/" + ressurs.Id
            });

            return punkt;
        }

        // Legger til telefon og e-post i kartets informasjonsboks.
        private void LeggTilKontakt(
            KartPunkt punkt,
            string? telefon,
            string? epost)
        {
            if (!string.IsNullOrWhiteSpace(telefon))
            {
                punkt.LeggTilLinje(
                    T["Telefon"],
                    telefon,
                    "tel:" + telefon.Replace(" ", "")
                );
            }

            if (!string.IsNullOrWhiteSpace(epost))
            {
                punkt.LeggTilLinje(
                    T["E-post"],
                    epost,
                    "mailto:" + epost
                );
            }
        }

        // Forkorter lange beskrivelser slik at kartets informasjonsbokser
        // ikke blir for store.
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