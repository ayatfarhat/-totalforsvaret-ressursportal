// Sanntidsoppdatering med SignalR.
// Serveren sender «BehovOppdatert» eller «RessurserOppdatert» til alle når noe endres
// (se Hubs/OppdateringsHub.cs og controllerne). Beskjeden inneholder ingen data,
// bare at noe er endret, så vi vet ikke om det gjelder akkurat denne listen.
//
// - Kartet på Kommandobro henter nye data med en gang (se kart.js).
// - På listene (behovskøen, «Mine behov» og ressurslisten) viser vi en melding med
//   knappen «Oppdater listen». Vi laster ikke siden på nytt av oss selv, for da
//   forsvinner det brukeren holder på med (filter, åpen historikk, hvor på siden man er).

// Kobler nettleseren til SignalR-serveren.
var tilkobling = new signalR.HubConnectionBuilder()
    .withUrl("/oppdateringer")
    .withAutomaticReconnect()
    .build();

// Listene som får meldingen, og hvilken beskjed de bryr seg om.
var listeSider = {
    "/behov/ko": "BehovOppdatert",
    "/behov/minebehov": "BehovOppdatert",
    "/ressurs/oversikt": "RessurserOppdatert"
};

// Viser meldingen øverst på siden. Kommer det flere endringer, vises den bare én gang.
function visEndringsmelding() {
    if (document.getElementById("endringsmelding")) {
        return;
    }

    var melding = document.createElement("div");
    melding.id = "endringsmelding";
    melding.className = "endringsmelding";
    melding.setAttribute("role", "status");

    var tekstdel = document.createElement("span");
    tekstdel.textContent = tekst("Det kan ha kommet endringer i listen.");

    var knapp = document.createElement("button");
    knapp.type = "button";
    knapp.className = "btn btn-primary btn-sm";
    knapp.textContent = tekst("Oppdater listen");
    knapp.addEventListener("click", function () {
        window.location.reload();
    });

    melding.appendChild(tekstdel);
    melding.appendChild(knapp);

    var innhold = document.getElementById("innhold");
    innhold.insertBefore(melding, innhold.firstChild);
}

// Kalles når serveren sier at noe er endret.
function noeErEndret(beskjed) {
    // Gir kartet beskjed om å hente nye data.
    document.dispatchEvent(new Event("kartdataOppdatert"));

    var side = window.location.pathname.toLowerCase();
    if (listeSider[side] === beskjed) {
        visEndringsmelding();
    }
}

tilkobling.on("RessurserOppdatert", function () {
    noeErEndret("RessurserOppdatert");
});

tilkobling.on("BehovOppdatert", function () {
    noeErEndret("BehovOppdatert");
});

// Starter tilkoblingen. Feiler den, prøver vi igjen etter 5 sekunder.
async function startSignalR() {
    try {
        await tilkobling.start();
    } catch (feil) {
        console.error("SignalR: Kunne ikke koble til", feil);
        setTimeout(startSignalR, 5000);
    }
}

startSignalR();
