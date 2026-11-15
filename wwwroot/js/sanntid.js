// Kobler nettleseren til SignalR-serveren.
const tilkobling = new signalR.HubConnectionBuilder()
    .withUrl("/oppdateringer")
    .withAutomaticReconnect()
    .build();

// Mottar beskjed når ressurser endres.
tilkobling.on("RessurserOppdatert", () => {
    console.log("SignalR: Ressurser er oppdatert");

//Gir kartet beskjed om å hente nye data. 
document.dispatchEvent(new Event('kartdataOppdatert'));

    // Oppdaterer bare ressursoversikten, ikke sider med skjemaer.
    if (window.location.pathname.toLowerCase() === "/ressurs/oversikt") {
        window.location.reload();
    }
});

// Mottar beskjed når behov endres.

tilkobling.on("BehovOppdatert", () => {

    console.log("SignalR: Behov er oppdatert");

    // Gir kartet beskjed om å hente nye data.

    document.dispatchEvent(new Event('kartdataOppdatert'));

    const side = window.location.pathname.toLowerCase();

    // Oppdaterer behovsoversiktene, men ikke skjemaer.

    if (side === "/behov/ko" || side === "/behov/minebehov") {

        window.location.reload();

    }

});

// Starter tilkoblingen.
async function startSignalR() {
    try {
        await tilkobling.start();
        console.log("SignalR: Tilkoblet");
    } catch (feil) {
        console.error("SignalR: Kunne ikke koble til", feil);
        setTimeout(startSignalR, 5000);
    }
}

startSignalR();