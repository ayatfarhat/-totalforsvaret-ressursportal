// Kart for Nabohjelp. Bruker Leaflet (kartbibliotek) og OpenStreetMap (selve kartet).
//
// Filen gjør tre ting, styrt av data-attributter i HTML-en:
//   1. Kartvelger  (data-kartvelger): brukeren klikker på kartet, og
//      breddegrad/lengdegrad fylles ut i skjemaet.
//   2. Visning av ett punkt (data-kartvisning): viser én nål, for eksempel på kvitteringen.
//   3. Oversikt for Kommandobro: funksjonen visOversikt(...) nederst.
//
// Hvis Leaflet ikke ble lastet (for eksempel uten internett), skjules kartet,
// og skjemaet kan fylles ut for hånd.

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
    var farge = element.getAttribute('data-nal');

    // Uten Leaflet skjuler vi kartet og knappen.
    if (typeof L === 'undefined') {
        element.hidden = true;
        knapp.hidden = true;
        statusTekst.textContent = 'Kartet kunne ikke lastes. Fyll ut feltene i stedet.';
        return;
    }

    var kart = lagKart(element, NORGE, 5);
    var nal = null;

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
        statusTekst.textContent = 'Valgt posisjon: ' + breddegrad.toFixed(5) + ', ' + lengdegrad.toFixed(5);

        // Hvis adressen er tom, fyller vi den med koordinatene.
        if (adresseFelt !== null && adresseFelt.value === '') {
            adresseFelt.value = 'Valgt på kart (' + breddegrad.toFixed(5) + ', ' + lengdegrad.toFixed(5) + ')';
        }
    }

    // Klikk på kartet
    kart.on('click', function (hendelse) {
        velgSted(hendelse.latlng.lat, hendelse.latlng.lng);
    });

    // Hvis skjemaet vises på nytt med koordinater (for eksempel etter en feil),
    // eller brukeren skriver dem inn selv, flytter vi nålen dit.
    function flyttNalFraFeltene() {
        var breddegrad = parseFloat(breddeFelt.value);
        var lengdegrad = parseFloat(lengdeFelt.value);
        if (!isNaN(breddegrad) && !isNaN(lengdegrad)) {
            velgSted(breddegrad, lengdegrad);
            kart.setView([breddegrad, lengdegrad], 13);
        }
    }
    breddeFelt.addEventListener('change', flyttNalFraFeltene);
    lengdeFelt.addEventListener('change', flyttNalFraFeltene);
    flyttNalFraFeltene();
    if (nal === null) {
        statusTekst.textContent = 'Ingen posisjon valgt ennå.';
    }

    // Knappen «Bruk min posisjon»
    knapp.addEventListener('click', function () {
        statusTekst.textContent = 'Henter posisjonen din ...';
        navigator.geolocation.getCurrentPosition(
            function (posisjon) {
                velgSted(posisjon.coords.latitude, posisjon.coords.longitude);
                kart.setView([posisjon.coords.latitude, posisjon.coords.longitude], 15);
            },
            function () {
                statusTekst.textContent = 'Fikk ikke tilgang til posisjonen din. Trykk på kartet i stedet.';
            }
        );
    });
}


// ---------- 2. Visning av ett punkt ----------

function startKartvisning(element) {
    if (typeof L === 'undefined') {
        element.hidden = true;
        return;
    }
    var breddegrad = parseFloat(element.getAttribute('data-bredde'));
    var lengdegrad = parseFloat(element.getAttribute('data-lengde'));

    var kart = lagKart(element, [breddegrad, lengdegrad], 14);
    L.marker([breddegrad, lengdegrad], { icon: lagNal('gi') }).addTo(kart);
}


// ---------- 3. Oversikt for Kommandobro ----------

// ressurser og behov er lister med punkter: { Tittel, Breddegrad, Lengdegrad }
function visOversikt(elementId, ressurser, behov) {
    var element = document.getElementById(elementId);
    if (typeof L === 'undefined') {
        element.hidden = true;
        return;
    }

    var kart = lagKart(element, NORGE, 5);
    var alleSteder = [];

    function leggTilNaler(liste, farge, prefiks) {
        liste.forEach(function (punkt) {
            var nal = L.marker([punkt.Breddegrad, punkt.Lengdegrad], { icon: lagNal(farge) }).addTo(kart);

            // Vi bruker textContent (ikke HTML-tekst), slik at teksten
            // aldri kan tolkes som HTML.
            var boks = document.createElement('div');
            boks.textContent = prefiks + ' ' + punkt.Tittel;
            nal.bindPopup(boks);

            alleSteder.push([punkt.Breddegrad, punkt.Lengdegrad]);
        });
    }

    leggTilNaler(ressurser, 'gi', 'Ressurs:');
    leggTilNaler(behov, 'hjelp', 'Behov:');

    // Zoomer kartet slik at alle nålene er med.
    if (alleSteder.length > 0) {
        kart.fitBounds(alleSteder, { padding: [48, 48], maxZoom: 12 });
    }
}


// ---------- Start ----------
// Scriptet ligger nederst på siden, så HTML-en er allerede lastet.

document.querySelectorAll('[data-kartvelger]').forEach(startKartvelger);
document.querySelectorAll('[data-kartvisning]').forEach(startKartvisning);
