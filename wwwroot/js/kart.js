// Kart for BeredskapsPortalen. Bruker Leaflet (kartbibliotek) og OpenStreetMap (selve kartet).
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
    flyttNalFraFeltene();
    if (nal === null) {
        statusTekst.textContent = 'Ingen posisjon valgt ennå.';
    }

    // Knappen «Bruk min posisjon»
    knapp.addEventListener('click', function () {
        // Noen nettlesere (eller sider uten https) har ikke posisjonstjenesten.
        if (!navigator.geolocation) {
            statusTekst.textContent = 'Nettleseren din støtter ikke posisjon. Trykk på kartet i stedet.';
            return;
        }

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

    function leggTilNaler(liste, farge, prefiks) {
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

            var nal = L.marker(posisjon, {
                icon: lagNal(farge)
            }).addTo(kart);

            var boks = document.createElement('div');
            boks.textContent = prefiks + ' ' + punkt.Tittel;
            nal.bindPopup(boks);

            alleMarkorer.push({
                markor: nal,
                posisjon: posisjon
            });

            alleSteder.push(posisjon);
        });
    }

    leggTilNaler(ressurser, 'gi', 'Ressurs:');
    leggTilNaler(behov, 'hjelp', 'Behov:');

    if (alleSteder.length > 0) {
        kart.fitBounds(alleSteder, {
            padding: [48, 48],
            maxZoom: 12
        });
    }

    function oppdaterFilter() {
        var valgtAvstand = filter ? filter.value : 'alle';
        var radius = valgtAvstand === 'alle'
            ? null
            : Number(valgtAvstand) * 1000;

        var antallSynlige = 0;

        alleMarkorer.forEach(function (punkt) {
            var skalVises = radius === null ||
                (valgtPosisjon !== null &&
                 valgtPosisjon.distanceTo(punkt.posisjon) <= radius);

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

            kart.fitBounds(radiusSirkel.getBounds(), {
                padding: [30, 30]
            });
        }

        if (status) {
            if (radius !== null && valgtPosisjon === null) {
                status.textContent =
                    'Klikk på kartet for å velge utgangspunkt.';
            } else if (radius === null) {
                status.textContent =
                    'Viser alle ' + antallSynlige + ' punkter.';
            } else {
                status.textContent =
                    'Viser ' + antallSynlige +
                    ' punkter innenfor ' +
                    (radius / 1000) + ' km.';
            }
        }
    }

    kart.on('click', function (hendelse) {
        valgtPosisjon = hendelse.latlng;

        if (startMarkor === null) {
            startMarkor = L.marker(valgtPosisjon, {
                draggable: true
            }).addTo(kart);

            startMarkor.bindPopup('Valgt utgangspunkt');

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
        filter.addEventListener('change', oppdaterFilter);
    }

    oppdaterFilter();
}


// ---------- Start ----------
// Scriptet ligger nederst på siden, så HTML-en er allerede lastet.

document.querySelectorAll('[data-kartvelger]').forEach(startKartvelger);
document.querySelectorAll('[data-kartvisning]').forEach(startKartvisning);
