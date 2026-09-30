# Kriseportal – ressurs- og behovsportal for Totalforsvaret

Prosjekt i IS-200 / IS-201 / IS-202 ved Universitetet i Agder, i samarbeid med Heimevernet og Kartverket.

Kriseportal skal koble behov fra offentlige aktører med ressurser fra innbyggere, frivillige og næringsliv under kriser. Denne versjonen er en ASP.NET Core MVC-applikasjon som kjører i Docker. Brukeren kan fylle ut et skjema og velge en posisjon i et kart, og dataene vises deretter på en egen side.

## Gruppemedlemmer og roller

<!-- Liste eller tabell avgjøres senere -->
| Oppgave | Gruppe-Medlem |
|---------|----------|
|Datamodell og Database|Torbjørn|
|Innlogging, roller og sikkerhet|Hassan|
|Behov og Statusflyt|Christian|
|Kart, tildeling og matching|Ayat|
|Ressurser + Design/layout|Marceli|
|Infrastruktur og Sanntid|Thea|


## Kjøre løsningen (drift)

### Forutsetninger

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) må være installert og startet
- [Git](https://git-scm.com/) for å hente koden

.NET SDK trengs ikke, siden applikasjonen bygges inne i Docker.

### Starte applikasjonen

```bash
git clone https://github.com/ayatfarhat/-totalforsvaret-ressursportal.git ressursportal
cd ressursportal
docker compose up --build
```

Åpne deretter <http://localhost:8080> i nettleseren.

Første gang tar det noen minutter, fordi Docker må laste ned .NET-bildene. Senere går det raskere.

### Stoppe applikasjonen

Trykk `Ctrl + C` i terminalen. For å fjerne containeren helt:

```bash
docker compose down
```

## Systemarkitektur

<!-- Skrives når koden er ferdig -->

## Testing



## Bruk av KI

<!-- Samlet for hele gruppa. Alle sender verktøy, bruk og eksempel på prompt til Thea -->
Vi brukte Claude (Anthropic) gjennom hele prosjektet, fra analyse til drift. I starten brukte vi det til å gå gjennom casen og tilbakemeldingen på Deliverable 1, og til å tolke kravene i Oppgave 1, blant annet for å se hvilke krav som manglet en ansvarlig i oppgavefordelingen. Et eksempel på prompt var «Ikke gjør noe. Start med å lese casen», etterfulgt av casedokumentet.

I arbeidet med Docker og drift brukte vi Claude til å sette opp Dockerfile, .dockerignore og docker-compose.yml, og til steg-for-steg-veiledning i Git med branches, pull requests og merge. Vi ba om ett steg om gangen og om forklaringer på hva hver linje gjorde, for eksempel «Hva gjør kodene...». Claude ble også brukt til feilsøking, blant annet da kloning feilet fordi et filnavn inneholdt kolon, og da æ, ø og å ble lagret med feil tegnkoding. I tillegg ga Claude forslag til kommentarer i Docker-filene og til struktur og drift-del i denne README-en.

KI var mest nyttig til å forstå hva kommandoer og feilmeldinger betydde. Forslagene ble testet ved å bygge og kjøre applikasjonen i Docker før de ble lagt inn i repoet.
