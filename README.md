# Totalforsvaret Ressursportal

IS-200 / IS-201 / IS-202 – Kriseberedskap, ressurs- og behovsportal for Totalforsvaret.

## Om Prosjektet

Nabohjelp er en ASP.NET Core MVC-applikasjon utviklet som et gruppeprosjekt.

Formålet med løsningen er å utvikle en webapplikasjon knyttet til kriseberedskap, ressurser og behov i Totalforsvaret.

## Gruppemedlemmer og Roller

| Ansvarsområde | Gruppemedlem |
|---|---|
| Datamodell og database | Torbjørn |
| Innlogging, roller og sikkerhet | Hassan |
| Behov og statusflyt | Christian |
| Kart, tildeling og matching | Ayat |
| Ressurser, design og layout | Marceli |
| Infrastruktur og sanntid | Thea |

## Drift og kjøring

### Forutsetninger

Applikasjonen kan kjøres på to måter:

| | Med Docker (anbefalt) | Uten Docker |
|---|---|---|
| **Må være installert** | [Docker Desktop](https://www.docker.com/products/docker-desktop/) og [Git](https://git-scm.com/) | [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0), Docker Desktop og Git |
| **Startes med** | `docker compose up --build` | `docker compose up -d mariadb` og `dotnet run` |
| **Åpnes på** | <http://localhost:8080> | <http://localhost:5144> |
| **Brukes til** | Vanlig kjøring og testing | Utvikling |

Docker Desktop må være startet før du kjører `docker compose`.

Begge bruker den samme MariaDB-databasen for brukere og behov. Ressurser lagres foreløpig bare i minnet, så de forsvinner når applikasjonen startes på nytt og deles ikke mellom de to kjøremåtene. Kjør bare én av dem om gangen.

### Starte applikasjonen med Docker

```bash
git clone https://github.com/ayatfarhat/-totalforsvaret-ressursportal.git ressursportal
cd ressursportal
docker compose up --build
```

Åpne deretter <http://localhost:8080> i nettleseren.

Første gang tar det noen minutter, fordi Docker må laste ned .NET- og MariaDB-bildene.

`docker compose` starter to containere: selve webapplikasjonen og en MariaDB-database. Applikasjonen kjører databasemigrasjoner automatisk ved oppstart, så databasen er klar til bruk med det samme.

### Stoppe Applikasjonen

Trykk `Ctrl + C` i terminalen. For å fjerne containerne helt:

```bash
docker compose down
```

Databasen beholdes i et Docker-volum. For å slette den også:

```bash
docker compose down -v
```

### Testbrukere

Ved oppstart opprettes tre testbrukere:

| E-post | Passord | Rolle |
|---|---|---|
| admin@test.no | Admin123! | Admin |
| kommune@test.no | Kommune123! | Kommune |
| privatperson@test.no | Privat123! | Frivillig |
<!-- | operator@test.no | Operator123! | Operator |
| public@test.no | Public123! | PublicActor | 

Rollene `Operator` og `PublicActor` kommer fra en tidligere versjon av databaseoppsettet og er ikke lenger i bruk noe sted i applikasjonen – de er kun beholdt som testbrukere inntil videre. All tilgangsstyring i appen (innlogging, Kommandobro, kartet, Behov) bruker `Admin`, `Kommune` og `Frivillig`. -->

Tilgangsstyringen i appen bruker rollene `Admin`, `Kommune` og `Frivillig`. Rollene `Operator` og `PublicActor` fra en tidligere versjon av databaseoppsettet er fjernet.

### Docker-oppsett

Applikasjonen bygges med en Dockerfile i to steg: først bygges appen med .NET SDK, deretter kopieres det ferdige resultatet over i et mindre bilde som bare kjører appen. Containeren lytter på port 8080.

Statiske filer (CSS, JavaScript og Bootstrap) ligger i standardmappen `wwwroot`, som `dotnet publish` tar med automatisk.

Databasen er MariaDB, som kjører i en egen container definert i `docker-compose.yml`. Data lagres i et eget Docker-volum og overlever både at containeren stoppes og startes på nytt.

### Kjøre uten Docker

Dette krever .NET 9 SDK. Databasen må fortsatt kjøre, og startes i Docker:

```bash
docker compose up -d mariadb
dotnet run
```

`-d` gjør at databasen kjører i bakgrunnen. Terminalen viser adressen applikasjonen kjører på, for eksempel:

```text
http://localhost:5144
```

## Teknologi

Prosjektet bruker:

- ASP.NET Core MVC
- .NET 9
- C#
- Razor
- HTML
- CSS
- JavaScript
- Bootstrap
- Leaflet
- OpenStreetMap
- MariaDB
- Entity Framework Core (migrasjoner)
- Docker og Docker Compose
- Git og GitHub

## Systemarkitektur

Prosjektet følger MVC-arkitekturen. Hver funksjon har sin egen Controller, View-mappe og (ved behov) en egen Model eller ViewModel:

| Funksjon | Controller | Views | Model / ViewModel |
|---|---|---|---|
| Innlogging og konto | `AccountController` | `Views/Account/` | `LoginViewModel`, `RegisterViewModel`, `KontoViewModel`, `TofaViewModel` |
| Ressurser | `RessursController` | `Views/Ressurs/` | `RessursFormViewModel` |
| Behov | `BehovController` | `Views/Behov/` | `Behov`, `BehovViewModel` |
| Tildeling og matching | `TildelingController` | `Views/Tildeling/` | `TildelingViewModel` |
| Kommandobro og kart | `KommandobroController` | `Views/Kommandobro/` | `MapViewModel`, `KartPunkt` |
| Forside | `HomeController` | `Views/Home/` | `HomeViewModel` |

Database-tilgangen går gjennom `ApplicationDbContext` i `Data/`, sammen med `SeedData.cs` (testbrukere) og `RoleInitializer.cs` (roller). Selve databasestrukturen styres av migrasjonsfilene i `Migrations/`.

## Kartfunksjonalitet

Kartet viser en sanntidsoversikt over alle registrerte ressurser og behov, direkte på Kommandobro-siden. Det er laget med Leaflet og bruker kartdata fra OpenStreetMap.

Kartet er ikke en egen side i navigasjonsmenyen – det er bare tilgjengelig for brukere med rollen `Admin` eller `Kommune`, på samme måte som resten av Kommandobro.

### Hvordan kartfunksjonen fungerer

1. Brukeren logger inn som `Admin` eller `Kommune` og åpner Kommandobro.
2. `KommandobroController` henter ressurser med registrert posisjon fra en midlertidig liste i minnet, og behov fra databasen.
3. Punktene sendes til viewet som JSON og tegnes på kartet med Leaflet.
4. Ressurser vises som grønne punkter, behov som røde punkter.
5. Brukeren kan klikke på et punkt for å se hva det gjelder.

Når en ny ressurs opprettes (`Views/Ressurs/Create.cshtml`), kan brukeren i tillegg velge posisjonen sin direkte på et interaktivt kart – et klikk plasserer en markør og fyller ut breddegrad/lengdegrad i skjemaet, som deretter lagres sammen med resten av ressursen.

## GET og POST

Applikasjonen håndterer både GET- og POST-forespørsler. Et eksempel er registrering av ressurs i `RessursController`:

- **GET:** Skjemaet for ny ressurs vises, med et interaktivt kart for å velge posisjon.
- **POST:** Når skjemaet sendes, mottas dataene og koordinatene gjennom `RessursFormViewModel`, og brukeren sendes videre til en bekreftelsesside som viser den valgte posisjonen på et kart.

## Responsivt design

Applikasjonen bruker Bootstrap for responsivt design.

Navigasjonsmenyen tilpasser seg størrelsen på skjermen. På mindre skjermer vises navigasjonen som en mobilmeny.

Kartet bruker hele den tilgjengelige bredden på kortet det står i, slik at det tilpasser seg forskjellige skjermstørrelser.

## Testing

Kartfunksjonen er testet manuelt underveis i utviklingen.

### Test 1 – Kartet vises på Kommandobro

**Handling:**
En bruker med rollen Admin eller Kommune logger inn og åpner Kommandobro.

**Forventet resultat:**
Leaflet-kartet skal vises innebygd på siden, med markører for registrerte ressurser og behov.

**Resultat:**
Bestått.

### Test 2 – Tilgangsstyring

**Handling:**
En bruker uten rollen Admin eller Kommune prøver å åpne Kommandobro.

**Forventet resultat:**
Brukeren skal ikke få tilgang til siden eller kartet.

**Resultat:**
Bestått (håndheves av `[Authorize(Roles = "Admin,Kommune")]` på `KommandobroController`).

### Test 3 – Velge posisjon ved oppretting av ressurs

**Handling:**
Brukeren fyller ut skjemaet for å opprette en ressurs og klikker på kartet for å velge posisjon.

**Forventet resultat:**
En markør skal vises der brukeren klikket, og koordinatene skal fylles ut i skjemaet.

**Resultat:**
Bestått.

### Test 4 – Lagring av koordinater

**Handling:**
Brukeren bekrefter og lagrer en ny ressurs med valgt posisjon.

**Forventet resultat:**
Ressursen skal dukke opp som en grønn markør på kartet på Kommandobro, på riktig posisjon.

**Resultat:**
Bør verifiseres på nytt etter siste sammenslåing med hovedbranchen (databaseoppsettet ble byttet fra SQLite til MariaDB underveis).

### Test 5 – Responsiv visning

**Handling:**
Applikasjonen åpnes i et smalt nettleservindu.

**Forventet resultat:**
Navigasjonen skal endres til en mobilmeny, og kortet med kartet skal fortsatt vises korrekt.

**Resultat:**
Bestått.

## Git og GitHub

Utviklingen gjøres med Git og GitHub.

Funksjonalitet utvikles på egne feature branches før den integreres med resten av prosjektet.

Kartfunksjonaliteten ble opprinnelig utviklet på `feature/map`, og senere videreutviklet og koblet inn i Kommandobro på `feature/ressurs-kart`. Dette gjorde det mulig å utvikle og teste kartfunksjonen uten å påvirke hovedbranchen direkte.

## Dokumentasjon i kode

Koden inneholder korte kommentarer som forklarer sentrale deler av implementasjonen.

Kommentarene brukes blant annet til å forklare:

- Opprettelse av kartet.
- Kartlaget.
- Markøren.
- Henting av koordinater.
- Lagring av koordinater.
- GET- og POST-metoder.
- Navigasjon og visning av data.

## Bruk av kunstig intelligens

KI har blitt brukt som et støtteverktøy under utviklingen av prosjektet.

KI har ikke blitt brukt som erstatning for testing av løsningen. Forslag til kode og løsninger har blitt kontrollert og testet i applikasjonen.

### KI-verktøy

Følgende KI-verktøy har blitt brukt:

- ChatGPT
- Claude 

### Bruksområder

KI har blant annet blitt brukt til:

**Idé og planlegging**
- Gjennomgang av casen og tolkning av kravene i oppgaven.
- Planlegging av MVC-strukturen og oppgavefordeling i gruppen.

**Utvikling**
- Forklaring av ASP.NET Core MVC, GET/POST og overføring av data med ViewModel.
- Implementering av Leaflet-kart og henting av koordinater.
- Utvikling av matching- og tildelingslogikk.
- Oppsett av MariaDB og Entity Framework, og tilkobling til applikasjonen.

**Docker, Git og drift**
- Oppsett av Dockerfile og docker-compose.yml, med forklaring av hvert steg.
- Branches, pull requests og samarbeid i GitHub.

**Feilsøking**
- Model binding og koordinater som ble vist som 0.
- Byggefeil, manglende pakker og database-feilmeldinger.
- Kloning som feilet på Windows, tegnkoding og manglende CSS i Docker.

**Dokumentasjon**
- Struktur i README, testscenarioer og kommentarer i koden.

### Eksempler på prompts

Under utviklingen ble KI blant annet spurt om:

- "Hvordan henter jeg latitude og longitude når brukeren klikker på et Leaflet-kart?"
- "Hvorfor blir Latitude og Longitude 0 etter POST i ASP.NET Core MVC?"
- "Hvordan kan behov og tilgjengelige ressurser matches etter type?"
- "Hvordan kan jeg implementere tildeling med GET og POST i ASP.NET Core MVC?"
- "Ett steg om gangen."
- "Hva gjør kodene..."
- "Hvorfor må navnet endres?"

### Hvordan KI-forslag ble kontrollert

KI-forslag ble ikke automatisk tatt i bruk.

Under utviklingen ble forslagene:

1. Lest og vurdert.
2. Lagt inn i prosjektet.
3. Kjørt lokalt.
4. Testet i nettleseren.
5. Feilsøkt dersom resultatet ikke var riktig.
6. Justert før fungerende kode ble beholdt.

Et eksempel var overføring av koordinater fra kartet. Kartet viste riktige koordinater, men resultatsiden viste først `0`. Network-verktøyet i nettleseren ble brukt for å kontrollere at koordinatene faktisk ble sendt med POST. Deretter ble problemet isolert til behandlingen av dataene i ASP.NET Core, og implementasjonen ble justert og testet på nytt.

I databasearbeidet fungerte ikke alle forslag med en gang. Flere løsninger måtte gjøres om, og feilmeldinger ble feilsøkt steg for steg før databasen fungerte i Docker.
