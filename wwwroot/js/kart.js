// Kart for BeredskapsPortalen. Bruker Leaflet (kartbibliotek) og OpenStreetMap (selve kartet).
//
// Filen gjør tre ting, styrt av data-attributter i HTML-en:
//   1. Kartvelger  (data-kartvelger): brukeren klikker på kartet, og
//      breddegrad/lengdegrad fylles ut i skjemaet. Kommunen og adressen
//      slås opp hos Kartverket og fylles inn (se finnStedet).
//   2. Visning av ett punkt (data-kartvisning): viser én nål, for eksempel på kvitteringen.
//   3. Oversikt for Kommandobro: funksjonen visOversikt(...) nederst.
//
// Hvis Leaflet ikke ble lastet (for eksempel uten internett), skjules kartet,
// og skjemaet kan fylles ut for hånd.
//
// Tekstene som vises, hentes med tekst('...'), som gir dem på valgt språk
// (se Views/Shared/_JsTekster.cshtml).

var NORGE = [64.5, 11.0];

// Lager selve kartet i et element og legger på kartbildene fra OpenStreetMap.
function lagKart(element, midtpunkt, zoom) {
    var kart = L.map(element, { scrollWheelZoom: false }).setView(midtpunkt, zoom);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(kart);
    return kart;
}

// Lager en kartnål. Utseendet (rødt eller grønt) ligger i site.css under .nal.
function lagNal(farge) {
    return L.divIcon({
        className: 'nal nal-' + farge,
        html: '<span></span>',
        iconSize: [28, 28],
        iconAnchor: [14, 34]
    });
}


// ---------- 1. Kartvelger ----------

function startKartvelger(element) {
    // Finner elementene som kartet skal samarbeide med.
    var breddeFelt = document.querySelector(element.getAttribute('data-bredde'));
    var lengdeFelt = document.querySelector(element.getAttribute('data-lengde'));
    var statusTekst = document.querySelector(element.getAttribute('data-status'));
    var knapp = document.querySelector(element.getAttribute('data-knapp'));
    var adresseFelt = null;
    if (element.getAttribute('data-posisjon')) {
        adresseFelt = document.querySelector(element.getAttribute('data-posisjon'));
    }
    var kommuneFelt = null;
    if (element.getAttribute('data-kommune')) {
        kommuneFelt = document.querySelector(element.getAttribute('data-kommune'));
    }
    var farge = element.getAttribute('data-nal');

    // Uten Leaflet skjuler vi kartet og knappen.
    if (typeof L === 'undefined') {
        element.hidden = true;
        knapp.hidden = true;
        statusTekst.textContent = tekst('Kartet kunne ikke lastes. Fyll ut feltene i stedet.');
        return;
    }

    var kart = lagKart(element, NORGE, 5);
    var nal = null;
    var slaOppStedet = true;   // om kommune og adresse skal slås opp når et sted velges

    // Flytter nålen til et sted og skriver koordinatene i skjemaet.
    function velgSted(breddegrad, lengdegrad) {
        if (nal === null) {
            nal = L.marker([breddegrad, lengdegrad], { icon: lagNal(farge), draggable: true }).addTo(kart);

            // Hvis brukeren drar nålen, oppdaterer vi koordinatene.
            nal.on('dragend', function () {
                var sted = nal.getLatLng();
                velgSted(sted.lat, sted.lng);
            });
        } else {
            nal.setLatLng([breddegrad, lengdegrad]);
        }

        breddeFelt.value = breddegrad.toFixed(6);
        lengdeFelt.value = lengdegrad.toFixed(6);
        statusTekst.textContent = tekst('Posisjon valgt.');

        if (slaOppStedet) {
            finnStedet(breddegrad, lengdegrad);
        }
    }

    // Poststedet kommer med store bokstaver (MO I RANA). Vi skriver det som «Mo I Rana»:
    // stor forbokstav i hvert ord, resten små.
    function storForbokstav(tekstMedStore) {
        var ord = tekstMedStore.toLowerCase().split(' ');
        for (var i = 0; i < ord.length; i++) {
            ord[i] = ord[i].charAt(0).toUpperCase() + ord[i].substring(1);
        }
        return ord.join(' ');
    }

    // Når kartet fyller inn et felt, ber vi skjemaet sjekke feltet på nytt (jQuery Validation,
    // som sidene med skjema laster inn). Da forsvinner en eventuell feilmelding om at feltet er tomt.
    function sjekkFeltet(felt) {
        if (window.jQuery && jQuery.fn.valid) {
            jQuery(felt).valid();
        }
    }

    // Slår opp kommunen og nærmeste adresse for punktet hos Kartverket (gratis, åpne
    // tjenester på ws.geonorge.no) og fyller dem inn i skjemaet. Feltene fylles bare
    // hvis de er tomme eller ble fylt inn av kartet tidligere, så vi aldri skriver over
    // noe brukeren har skrevet selv. Går oppslaget galt, skriver brukeren inn selv.
    var forrigeKommune = '';
    var forrigeAdresse = '';
    var oppslagNummer = 0;

    function finnStedet(breddegrad, lengdegrad) {
        // Klikker brukeren flere ganger fort, bruker vi bare svaret på det siste klikket.
        oppslagNummer = oppslagNummer + 1;
        var detteOppslaget = oppslagNummer;

        if (kommuneFelt !== null) {
            statusTekst.textContent = tekst('Finner kommunen ...');
            fetch('https://ws.geonorge.no/kommuneinfo/v1/punkt?koordsys=4258&nord=' + breddegrad + '&ost=' + lengdegrad)
                .then(function (svar) {
                    if (!svar.ok) { throw new Error('Fant ingen kommune'); }
                    return svar.json();
                })
                .then(function (data) {
                    if (detteOppslaget !== oppslagNummer) { return; }
                    // Navnet hentes fra vår egen liste (datalist), så det blir likt som ellers på siden.
                    var valg = document.querySelector('#kommuneliste option[data-nummer="' + data.kommunenummer + '"]');
                    if (valg === null) { throw new Error('Ukjent kommune'); }

                    if (kommuneFelt.value === '' || kommuneFelt.value === forrigeKommune) {
                        kommuneFelt.value = valg.value;
                        forrigeKommune = valg.value;
                        sjekkFeltet(kommuneFelt);
                    }
                    statusTekst.textContent = tekst('Posisjon valgt i {0} kommune.', valg.value);
                })
                .catch(function () {
                    if (detteOppslaget !== oppslagNummer) { return; }
                    statusTekst.textContent = tekst('Posisjon valgt. Fant ikke kommunen, skriv den inn selv.');
                });
        }

        if (adresseFelt !== null && (adresseFelt.value === '' || adresseFelt.value === forrigeAdresse)) {
            fetch('https://ws.geonorge.no/adresser/v1/punktsok?radius=150&treffPerSide=1&lat=' + breddegrad + '&lon=' + lengdegrad)
                .then(function (svar) {
                    if (!svar.ok) { throw new Error('Fant ingen adresse'); }
                    return svar.json();
                })
                .then(function (data) {
                    if (detteOppslaget !== oppslagNummer || !data.adresser || data.adresser.length === 0) { return; }
                    var adresse = data.adresser[0];
                    adresseFelt.value = adresse.adressetekst + ', ' + storForbokstav(adresse.poststed);
                    forrigeAdresse = adresseFelt.value;
                    sjekkFeltet(adresseFelt);
                })
                .catch(function () {
                    // Ingen adresse i nærheten (for eksempel i skogen): brukeren skriver selv.
                });
        }
    }

    // Klikk på kartet
    kart.on('click', function (hendelse) {
        velgSted(hendelse.latlng.lat, hendelse.latlng.lng);
    });

    // Hvis skjemaet vises på nytt med koordinater (for eksempel etter en feil),
    // eller brukeren skriver dem inn selv, flytter vi nålen dit.
    // Nordmenn skriver ofte komma som desimaltegn (58,14656). Serveren og
    // kartet forventer punktum, så vi bytter det ut før vi bruker verdiene.
    function rensKoordinater() {
        breddeFelt.value = breddeFelt.value.replace(',', '.').trim();
        lengdeFelt.value = lengdeFelt.value.replace(',', '.').trim();
    }

    function flyttNalFraFeltene() {
        rensKoordinater();
        var breddegrad = parseFloat(breddeFelt.value);
        var lengdegrad = parseFloat(lengdeFelt.value);

        // Koordinater utenfor jordkloden ignoreres (serveren gir feilmelding).
        var gyldig = !isNaN(breddegrad) && !isNaN(lengdegrad)
            && breddegrad >= -90 && breddegrad <= 90
            && lengdegrad >= -180 && lengdegrad <= 180;

        if (gyldig) {
            velgSted(breddegrad, lengdegrad);
            kart.setView([breddegrad, lengdegrad], 13);
        }
    }
    breddeFelt.addEventListener('change', flyttNalFraFeltene);
    lengdeFelt.addEventListener('change', flyttNalFraFeltene);

    // Rydder også når skjemaet sendes (for eksempel hvis brukeren trykker Enter).
    var skjema = element.closest('form');
    if (skjema !== null) {
        skjema.addEventListener('submit', rensKoordinater);
    }
    // Når siden åpnes med koordinater (for eksempel ved redigering), står kommunen og
    // adressen der allerede. Da slår vi ikke opp stedet, bare når brukeren velger et nytt.
    slaOppStedet = false;
    flyttNalFraFeltene();
    slaOppStedet = true;
    if (nal === null) {
        statusTekst.textContent = tekst('Ingen posisjon valgt ennå.');
    }

    // Knappen «Bruk min posisjon»
    knapp.addEventListener('click', function () {
        // Noen nettlesere (eller sider uten https) har ikke posisjonstjenesten.
        if (!navigator.geolocation) {
            statusTekst.textContent = tekst('Nettleseren din støtter ikke posisjon. Trykk på kartet i stedet.');
            return;
        }

        statusTekst.textContent = tekst('Henter posisjonen din ...');
        navigator.geolocation.getCurrentPosition(
            function (posisjon) {
                velgSted(posisjon.coords.latitude, posisjon.coords.longitude);
                kart.setView([posisjon.coords.latitude, posisjon.coords.longitude], 15);
            },
            function () {
                statusTekst.textContent = tekst('Fikk ikke tilgang til posisjonen din. Trykk på kartet i stedet.');
            }
        );
    });
}


// ---------- 2. Visning av ett punkt ----------

function startKartvisning(element) {
    var breddegrad = parseFloat(element.getAttribute('data-bredde'));
    var lengdegrad = parseFloat(element.getAttribute('data-lengde'));
    var farge = element.getAttribute('data-nal') || 'gi';   // 'gi' = grønn, 'hjelp' = rød

    // Uten Leaflet, eller uten en ekte posisjon, viser vi ikke noe kart.
    var gyldig = !isNaN(breddegrad) && !isNaN(lengdegrad) && !(breddegrad === 0 && lengdegrad === 0);
    if (typeof L === 'undefined' || !gyldig) {
        element.hidden = true;
        return;
    }

    var kart = lagKart(element, [breddegrad, lengdegrad], 14);
    L.marker([breddegrad, lengdegrad], { icon: lagNal(farge) }).addTo(kart);
}


// ---------- 3. Oversikt for Kommandobro ----------

// ressurser og behov er lister med punkter fra KommandobroController:
// { Slag, Tittel, Breddegrad, Lengdegrad, Farge, Merker, Undertekst, Beskrivelse,
//   Linjer: [{ Etikett, Verdi, Lenke }], Fremhevet, FremhevetErFunnet, Lenker: [{ Tekst, Adresse }] }
// (se ViewModels/MapViewModel.cs)
function visOversikt(elementId, ressurser, behov) {
    var element = document.getElementById(elementId);

    if (!element || typeof L === 'undefined') {
        if (element) element.hidden = true;
        return;
    }

    var kart = lagKart(element, NORGE, 5);
    var filter = document.getElementById('avstandFilter');
    var status = document.getElementById('filterStatus');

    var alleMarkorer = [];
    var alleSteder = [];
    var valgtPosisjon = null;
    var startMarkor = null;
    var radiusSirkel = null;

    // Lager boksen som vises når man trykker på en prikk.
    // Vi bruker textContent (ikke innerHTML), så tekst brukerne har skrevet
    // aldri kan bli tolket som HTML eller skript.
    function lagInfoboks(punkt) {
        var boks = document.createElement('div');
        boks.className = 'kart-info';

        // Øverst: «Aktivt behov» eller «Ressurs», og så tittelen.
        leggTilTekst(boks, 'span', 'kart-info-slag', punkt.Slag);
        leggTilTekst(boks, 'strong', 'kart-info-tittel', punkt.Tittel);

        // Merkene, for eksempel «Akutt» og «Ny».
        if (punkt.Merker.length > 0) {
            var merker = document.createElement('div');
            merker.className = 'kart-info-merker';
            punkt.Merker.forEach(function (merke) {
                leggTilTekst(merker, 'span', 'merke ' + merke.Farge, merke.Tekst);
            });
            boks.appendChild(merker);
        }

        leggTilTekst(boks, 'p', 'kart-info-under', punkt.Undertekst);
        leggTilTekst(boks, 'p', 'kart-info-beskrivelse', punkt.Beskrivelse);

        // Telefon, e-post og annet: etikett til venstre, verdi til høyre.
        if (punkt.Linjer.length > 0) {
            var liste = document.createElement('dl');
            punkt.Linjer.forEach(function (linje) {
                leggTilTekst(liste, 'dt', '', linje.Etikett);
                var verdi = document.createElement('dd');
                if (linje.Lenke) {
                    leggTilTekst(verdi, 'a', '', linje.Verdi).href = linje.Lenke;
                } else {
                    verdi.textContent = linje.Verdi;
                }
                liste.appendChild(verdi);
            });
            boks.appendChild(liste);
        }

        // Nærmeste ressurs: grønn boks hvis en ble funnet, ellers grå.
        if (punkt.Fremhevet) {
            var klasse = punkt.FremhevetErFunnet ? 'kart-info-fremhevet funnet' : 'kart-info-fremhevet';
            leggTilTekst(boks, 'p', klasse, punkt.Fremhevet);
        }

        if (punkt.Lenker.length > 0) {
            var knapper = document.createElement('div');
            knapper.className = 'kart-info-knapper';
            punkt.Lenker.forEach(function (lenke) {
                leggTilTekst(knapper, 'a', '', lenke.Tekst).href = lenke.Adresse;
            });
            boks.appendChild(knapper);
        }

        return boks;
    }

    // Lager et element med tekst og legger det til i forelder. Tom tekst hoppes over.
    // Gir elementet tilbake, så vi for eksempel kan sette href på en lenke.
    function leggTilTekst(forelder, tagg, klasse, tekst) {
        var element = document.createElement(tagg);
        if (!tekst) {
            return element;
        }
        if (klasse) {
            element.className = klasse;
        }
        element.textContent = tekst;
        forelder.appendChild(element);
        return element;
    }

    function leggTilNaler(liste) {
        liste.forEach(function (punkt) {
            var breddegrad = Number(punkt.Breddegrad);
            var lengdegrad = Number(punkt.Lengdegrad);

            if (!Number.isFinite(breddegrad) ||
                !Number.isFinite(lengdegrad) ||
                breddegrad < -90 || breddegrad > 90 ||
                lengdegrad < -180 || lengdegrad > 180) {
                return;
            }

            var posisjon = L.latLng(breddegrad, lengdegrad);

            // title vises når man holder musen over prikken, og leses opp av skjermlesere.
            var nal = L.marker(posisjon, {
                icon: lagNal(punkt.Farge),
                title: punkt.Tittel,
                alt: punkt.Tittel
            }).addTo(kart);

            // Plass til venstre, så boksen ikke havner under knappene for zoom (+/−).
            nal.bindPopup(lagInfoboks(punkt), { minWidth: 250, maxWidth: 320, autoPanPaddingTopLeft: [60, 20] });

            alleMarkorer.push({
                markor: nal,
                posisjon: posisjon
            });

            alleSteder.push(posisjon);
        });
    }

    leggTilNaler(ressurser);
    leggTilNaler(behov);

    if (alleSteder.length > 0) {
        kart.fitBounds(alleSteder, {
            padding: [48, 48],
            maxZoom: 12
        });
    }
// Henter oppdaterte kartpunkter fra serveren.
// Funksjonen erstatter gamle markører med nye,
// men beholder kartet, zoomnivået og valgt avstandsfilter.
function oppdaterKartpunkter(nyeRessurser, nyeBehov) {

    // Fjerner de gamle markørene fra kartet.
    alleMarkorer.forEach(function (punkt) {
        kart.removeLayer(punkt.markor);
    });

    // Tømmer listene med gamle markører og posisjoner.
    alleMarkorer = [];
    alleSteder = [];

    // Legger til nye markører med oppdatert informasjon.
    leggTilNaler(nyeRessurser);
    leggTilNaler(nyeBehov);

    // Bruker det eksisterende avstandsfilteret på de nye punktene.
    // Kartet opprettes ikke på nytt.
    oppdaterFilter(false);
}

// Hindrer at flere forespørsler kjøres samtidig.
var henterKartdata = false;

// Henter oppdaterte behov og ressurser fra KommandobroController.
async function hentOppdaterteKartdata() {

    // Starter ikke en ny forespørsel hvis den forrige fortsatt kjører.
    if (henterKartdata) {
        return;
    }

    henterKartdata = true;

    try {
        // Henter oppdaterte kartdata fra API-endepunktet.
        var svar = await fetch('/Kommandobro/Kartdata', {
            method: 'GET',
            cache: 'no-store'
        });

        // Kontrollerer at serveren svarte uten feil.
        if (!svar.ok) {
            throw new Error('Kunne ikke hente kartdata: ' + svar.status);
        }

        // Gjør JSON-svaret om til et JavaScript-objekt.
        var data = await svar.json();

        // Kontrollerer at serveren returnerte forventede lister.
        if (!Array.isArray(data.ressurser) || !Array.isArray(data.behov)) {
            throw new Error('Ugyldig format på kartdata.');
        }

        // Oppdaterer markørene med de nyeste dataene.
        oppdaterKartpunkter(data.ressurser, data.behov);

    } catch (feil) {

        // Skriver feilen i nettleserens konsoll.
        // Eksisterende kartpunkter beholdes hvis hentingen feiler.
        console.error('Feil ved oppdatering av kart:', feil);

    } finally {

        // Tillater neste forespørsel når denne er ferdig.
        henterKartdata = false;
    }
}
     // Filtrerer kartpunkter etter valgt avstand.
// Parameteren bestemmer om kartutsnittet skal justeres.
function oppdaterFilter(justerKart = true) {
        var valgtAvstand = filter ? filter.value : 'alle';
        var radius = valgtAvstand === 'alle'
            ? null
            : Number(valgtAvstand) * 1000;

        var antallSynlige = 0;

        // Alle punkter vises til brukeren har valgt både avstand og et utgangspunkt.
        var filtrerer = radius !== null && valgtPosisjon !== null;

        alleMarkorer.forEach(function (punkt) {
            var skalVises = !filtrerer ||
                valgtPosisjon.distanceTo(punkt.posisjon) <= radius;

            if (skalVises) {
                if (!kart.hasLayer(punkt.markor)) {
                    punkt.markor.addTo(kart);
                }
                antallSynlige++;
            } else {
                kart.removeLayer(punkt.markor);
            }
        });

        if (radiusSirkel !== null) {
            kart.removeLayer(radiusSirkel);
            radiusSirkel = null;
        }

        if (radius !== null && valgtPosisjon !== null) {
            radiusSirkel = L.circle(valgtPosisjon, {
                radius: radius,
                color: '#1B6B4B',
                fillOpacity: 0.08
            }).addTo(kart);

           // Justerer bare kartutsnittet når brukeren selv endrer filteret.
// Automatisk oppdatering skal ikke flytte eller zoome kartet.
if (justerKart) {
    kart.fitBounds(radiusSirkel.getBounds(), {
        padding: [30, 30]
    });
}

// Avslutter betingelsen som tegner avstandssirkelen.
}

if (status) {
            // «1 punkt» eller «3 punkter» (tekst() kommer fra Views/Shared/_JsTekster.cshtml).
            var punkter = antallSynlige === 1 ? tekst('1 punkt') : tekst('{0} punkter', antallSynlige);
            if (radius !== null && valgtPosisjon === null) {
                status.textContent = tekst('Klikk på kartet for å velge utgangspunkt. Viser alle {0} så lenge.', punkter);
            } else if (radius === null) {
                status.textContent = tekst('Viser alle {0}.', punkter);
            } else {
                status.textContent = tekst('Viser {0} innenfor {1} km.', punkter, radius / 1000);
            }
        }
    }

    kart.on('click', function (hendelse) {
        valgtPosisjon = hendelse.latlng;

        if (startMarkor === null) {
            startMarkor = L.marker(valgtPosisjon, {
                draggable: true
            }).addTo(kart);

            startMarkor.bindPopup(tekst('Valgt utgangspunkt'));

            startMarkor.on('dragend', function () {
                valgtPosisjon = startMarkor.getLatLng();
                oppdaterFilter();
            });
        } else {
            startMarkor.setLatLng(valgtPosisjon);
        }

        oppdaterFilter();
    });

    if (filter) {
    // Oppdaterer kartet når brukeren endrer avstandsfilteret.
    filter.addEventListener('change', function () {
        oppdaterFilter();
    });
}

// Viser riktig antall punkter når kartet åpnes.
oppdaterFilter();

// Henter oppdaterte ressurser og behov hvert 15. sekund.
setInterval(function () {

    // Henter bare data når nettleserfanen er synlig.
    if (!document.hidden) {
        hentOppdaterteKartdata();
    }

}, 15000);

// Henter nye data når brukeren kommer tilbake til fanen.
document.addEventListener('visibilitychange', function () {
    if (!document.hidden) {
        hentOppdaterteKartdata();
    }
});

} // Slutten på visOversikt()


// ---------- Start ----------
// Scriptet ligger nederst på siden, så HTML-en er allerede lastet.

document.querySelectorAll('[data-kartvelger]').forEach(startKartvelger);
document.querySelectorAll('[data-kartvisning]').forEach(startKartvisning);
