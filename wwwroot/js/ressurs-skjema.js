// Små hjelpefunksjoner for skjemaene "Registrer en ressurs", "Meld behov"
// og "Legg til ønsket ressurs". Autofyllen virker på alle typevalg med
// data-forslag. Hver del gjør ingenting på sider som mangler feltene sine.
(function () {
    // ---- 1) Autofyll av beskrivelse når brukeren velger type ----
    var beskrivelse = document.getElementById("Beskrivelse");
    var typevalg = document.querySelectorAll(".type-valg input[data-forslag]");

    // Husker det siste forslaget vi fylte inn. Så vet vi om brukeren har
    // endret teksten selv (da lar vi den være i fred).
    var sisteForslag = "";

    for (var i = 0; i < typevalg.length; i++) {
        typevalg[i].addEventListener("change", function (hendelse) {
            if (!beskrivelse) { return; }

            var nyttForslag = hendelse.target.getAttribute("data-forslag") || "";
            var tomEllerUendret = beskrivelse.value.trim() === "" || beskrivelse.value === sisteForslag;

            if (tomEllerUendret) {
                beskrivelse.value = nyttForslag;
                sisteForslag = nyttForslag;
            }
        });
    }

    // ---- 2) Bilde: sjekk størrelse og vis forhåndsvisning ----
    var maksStorrelse = 5 * 1024 * 1024; // 5 MB, samme grense som på serveren
    var filfelt = document.getElementById("bildefil");
    var visning = document.getElementById("bildeForhandsvisning");
    var feilTekst = document.getElementById("bildeFeil");

    if (filfelt && visning && feilTekst) {
        filfelt.addEventListener("change", function () {
            feilTekst.hidden = true;
            visning.hidden = true;

            var fil = filfelt.files[0];
            if (!fil) { return; }

            if (fil.size > maksStorrelse) {
                feilTekst.textContent = tekst("Bildet er for stort. Maks størrelse er 5 MB.");
                feilTekst.hidden = false;
                filfelt.value = "";
                return;
            }

            if (!fil.type.startsWith("image/")) {
                feilTekst.textContent = tekst("Filen er ikke et bilde.");
                feilTekst.hidden = false;
                filfelt.value = "";
                return;
            }

            visning.src = URL.createObjectURL(fil);
            visning.hidden = false;
        });
    }

    // ---- 3) Fritekst for «Annet»: feltet vises bare når «Annet» er valgt ----
    var annetFelt = document.getElementById("annet-felt");
    var typeKnapper = document.querySelectorAll('input[name="Type"]');

    function visAnnetFelt() {
        var valgt = document.querySelector('input[name="Type"]:checked');
        annetFelt.hidden = !valgt || valgt.value !== "Annet";
    }

    if (annetFelt) {
        for (var j = 0; j < typeKnapper.length; j++) {
            typeKnapper[j].addEventListener("change", visAnnetFelt);
        }
        visAnnetFelt();
    }
})();
