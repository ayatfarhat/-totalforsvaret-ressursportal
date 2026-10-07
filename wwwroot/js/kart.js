/* Kart for Nabohjelp (Leaflet + OpenStreetMap).
   Brukes på tre måter, styrt av data-attributter i viewene:

   1. Kartvelger (data-kartvelger): brukeren klikker, drar markøren eller
      trykker «Bruk min posisjon», og breddegrad/lengdegrad fylles i skjemaet.
        data-bredde     velger for feltet med breddegrad (påkrevd)
        data-lengde     velger for feltet med lengdegrad (påkrevd)
        data-status     (valgfritt) elementet som viser valgt posisjon som tekst
        data-posisjon   (valgfritt) tekstfelt som fylles ut hvis det er tomt
        data-knapp      (valgfritt) knappen «Bruk min posisjon»
        data-nal        «gi» (grønn, standard) eller «hjelp» (rød)
   2. Visning av ett punkt (data-kartvisning): data-bredde og data-lengde er tallene.
   3. Oversikt for Kommandobro: NabohjelpKart.oversikt(id, ressurser, behov).

   Siden skjemaet fungerer uten kart, viser vi bare en kort melding hvis
   Leaflet ikke ble lastet (for eksempel uten nett). */

(function () {
    'use strict';

    var NORGE = [64.5, 11.0];
    var FLISER = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
    var KILDE = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';

    function lagKart(el, midt, zoom) {
        // Rullehjulet zoomer ikke kartet, slik at siden ikke «fanges» av kartet.
        var kart = L.map(el, { scrollWheelZoom: false }).setView(midt, zoom);
        L.tileLayer(FLISER, { maxZoom: 19, attribution: KILDE }).addTo(kart);
        return kart;
    }

    // Kartnål tegnet i CSS (.nal i site.css), så vi slipper bildefiler.
    function nal(type) {
        return L.divIcon({
            className: 'nal nal-' + type,
            html: '<span></span>',
            iconSize: [28, 28],
            iconAnchor: [14, 34],
            popupAnchor: [0, -34]
        });
    }

    function tall(verdi) {
        var n = parseFloat(String(verdi).replace(',', '.'));
        return isFinite(n) ? n : null;
    }

    function finn(velger) {
        return velger ? document.querySelector(velger) : null;
    }

    function valider(felt) {
        // Fjerner en eventuell feilmelding når feltet nå har en verdi.
        try {
            if (window.jQuery && jQuery.fn.valid) { jQuery(felt).valid(); }
        } catch (e) { /* valideringen er bare en bonus */ }
    }

    /* ---------- 1. Kartvelger ---------- */

    function velger(el) {
        var bredde = finn(el.getAttribute('data-bredde'));
        var lengde = finn(el.getAttribute('data-lengde'));
        var status = finn(el.getAttribute('data-status'));
        var posisjon = finn(el.getAttribute('data-posisjon'));
        var knapp = finn(el.getAttribute('data-knapp'));
        var type = el.getAttribute('data-nal') || 'gi';
        if (!bredde || !lengde) { return; }

        function melding(tekst) {
            if (status) { status.textContent = tekst; }
        }

        if (typeof L === 'undefined') {
            melding('Kartet kunne ikke lastes. Du kan fylle ut feltene i stedet.');
            el.hidden = true;
            if (knapp) { knapp.hidden = true; }
            return;
        }

        var kart = lagKart(el, NORGE, 5);
        var markor = null;

        function visValgt(lat, lng) {
            if (!status) { return; }
            status.textContent = '';
            var sterk = document.createElement('strong');
            sterk.textContent = 'Valgt posisjon: ';
            status.appendChild(sterk);
            status.appendChild(document.createTextNode(lat.toFixed(5) + ', ' + lng.toFixed(5)));
        }

        function settPunkt(lat, lng, valg) {
            valg = valg || {};
            if (markor === null) {
                markor = L.marker([lat, lng], { icon: nal(type), draggable: true, keyboard: true, title: 'Valgt posisjon. Dra for å flytte.' }).addTo(kart);
                markor.on('dragend', function () {
                    var p = markor.getLatLng();
                    settPunkt(p.lat, p.lng, { skrivFelt: true });
                });
            } else {
                markor.setLatLng([lat, lng]);
            }

            if (valg.skrivFelt) {
                bredde.value = lat.toFixed(6);
                lengde.value = lng.toFixed(6);
                valider(bredde);
                valider(lengde);
                if (posisjon && !posisjon.value.trim()) {
                    posisjon.value = 'Valgt på kart (' + lat.toFixed(5) + ', ' + lng.toFixed(5) + ')';
                    valider(posisjon);
                }
            }
            if (valg.flytt) { kart.setView([lat, lng], valg.zoom || kart.getZoom()); }
            visValgt(lat, lng);
        }

        kart.on('click', function (e) {
            settPunkt(e.latlng.lat, e.latlng.lng, { skrivFelt: true });
        });

        // Feltene kan også skrives i for hånd.
        function fraFelt() {
            var lat = tall(bredde.value);
            var lng = tall(lengde.value);
            if (lat !== null && lng !== null && Math.abs(lat) <= 90 && Math.abs(lng) <= 180) {
                settPunkt(lat, lng, { flytt: true, zoom: 13 });
            }
        }
        if (bredde.type !== 'hidden') {
            bredde.addEventListener('change', fraFelt);
            lengde.addEventListener('change', fraFelt);
        }

        // Skjemaet vises på nytt med verdier hvis noe var feil. Da viser vi dem på kartet.
        fraFelt();
        if (markor === null) { melding('Ingen posisjon valgt ennå.'); }

        if (knapp) {
            if (!navigator.geolocation) {
                knapp.hidden = true;
            } else {
                knapp.addEventListener('click', function () {
                    melding('Henter posisjonen din ...');
                    knapp.disabled = true;
                    navigator.geolocation.getCurrentPosition(function (pos) {
                        knapp.disabled = false;
                        settPunkt(pos.coords.latitude, pos.coords.longitude, { skrivFelt: true, flytt: true, zoom: 15 });
                    }, function () {
                        knapp.disabled = false;
                        melding('Fikk ikke tilgang til posisjonen din. Trykk på kartet i stedet.');
                    }, { enableHighAccuracy: true, timeout: 10000 });
                });
            }
        }
    }

    /* ---------- 2. Ett punkt ---------- */

    function visning(el) {
        if (typeof L === 'undefined') { el.hidden = true; return; }
        var lat = tall(el.getAttribute('data-bredde'));
        var lng = tall(el.getAttribute('data-lengde'));
        if (lat === null || lng === null) { el.hidden = true; return; }
        var kart = lagKart(el, [lat, lng], 14);
        L.marker([lat, lng], { icon: nal(el.getAttribute('data-nal') || 'gi'), keyboard: false }).addTo(kart);
    }

    /* ---------- 3. Oversikt for Kommandobro ---------- */

    function oversikt(id, ressurser, behov) {
        var el = document.getElementById(id);
        if (!el) { return; }
        if (typeof L === 'undefined') {
            el.hidden = true;
            return;
        }

        var kart = lagKart(el, NORGE, 5);
        var punkter = [];

        function legg(liste, type, tittel) {
            liste.forEach(function (p) {
                var markor = L.marker([p.Breddegrad, p.Lengdegrad], { icon: nal(type), title: tittel + ' ' + p.Tittel }).addTo(kart);

                // Popup bygges som elementer (ikke tekststreng), så innholdet aldri tolkes som HTML.
                var boks = document.createElement('div');
                var sterk = document.createElement('strong');
                sterk.textContent = tittel;
                boks.appendChild(sterk);
                boks.appendChild(document.createTextNode(' ' + p.Tittel));
                markor.bindPopup(boks);

                punkter.push([p.Breddegrad, p.Lengdegrad]);
            });
        }

        legg(ressurser, 'gi', 'Ressurs:');
        legg(behov, 'hjelp', 'Behov:');

        if (punkter.length > 0) {
            kart.fitBounds(punkter, { padding: [48, 48], maxZoom: 12 });
        }
    }

    window.NabohjelpKart = { oversikt: oversikt };

    function start() {
        Array.prototype.forEach.call(document.querySelectorAll('[data-kartvelger]'), velger);
        Array.prototype.forEach.call(document.querySelectorAll('[data-kartvisning]'), visning);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
})();
