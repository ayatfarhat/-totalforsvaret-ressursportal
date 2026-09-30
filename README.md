# Totalforsvaret Ressursportal

IS-200 / IS-201 / IS-202 – Kriseberedskap, ressurs- og behovsportal for Totalforsvaret.

## Om prosjektet

Nabohjelp er en ASP.NET Core MVC-applikasjon utviklet som et gruppeprosjekt.

Formålet med løsningen er å utvikle en webapplikasjon knyttet til kriseberedskap, ressurser og behov i Totalforsvaret.

## Gruppemedlemmer og roller

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

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) må være installert og startet
- [Git](https://git-scm.com/) for å hente koden

.NET SDK trengs ikke, siden applikasjonen bygges inne i Docker.

### Starte applikasjonen med Docker

```bash
git clone https://github.com/ayatfarhat/-totalforsvaret-ressursportal.git ressursportal
cd ressursportal
docker compose up --build
```

Åpne deretter <http://localhost:8080> i nettleseren.

Første gang tar det noen minutter, fordi Docker må laste ned .NET- og MariaDB-bildene.

`docker compose` starter to containere: selve webapplikasjonen og en MariaDB-database. Applikasjonen kjører databasemigrasjoner automatisk ved oppstart, så databasen er klar til bruk med det samme.

### Stoppe applikasjonen

Trykk `Ctrl + C` i terminalen. For å fjerne containerne helt (inkludert databasen):

```bash
docker compose down
```

### Testbrukere

Ved oppstart opprettes fem testbrukere:

| E-post | Passord | Rolle |
|---|---|---|
| admin@test.no | Admin123! | Admin |
| kommune@test.no | Kommune123! | Kommune |
| privatperson@test.no | Privat123! | Frivillig |
| operator@test.no | Operator123! | Operator |
| public@test.no | Public123! | PublicActor |

Rollene `Operator` og `PublicActor` kommer fra en tidligere versjon av databaseoppsettet og er ikke lenger i bruk noe sted i applikasjonen – de er kun beholdt som testbrukere inntil videre. All tilgangsstyring i appen (innlogging, Kommandobro, kartet, Behov) bruker `Admin`, `Kommune` og `Frivillig`.

### Docker-oppsett

Applikasjonen bygges med en Dockerfile i to steg: først bygges appen med .NET SDK, deretter kopieres det ferdige resultatet over i et mindre bilde som bare kjører appen. Containeren lytter på port 8080.

Statiske filer (CSS, JavaScript og Bootstrap) ligger i standardmappen `wwwroot`, som `dotnet publish` tar med automatisk.

Databasen er MariaDB, som kjører i en egen container definert i `docker-compose.yml`. Data lagres i et eget Docker-volum og overlever både at containeren stoppes og startes på nytt.

### Kjøre uten Docker

Prosjektet kan startes lokalt med .NET SDK, men da må en MariaDB-database være tilgjengelig separat (for eksempel startet med `docker compose up mariadb`), siden applikasjonen ikke lenger har noen innebygd fil-database å falle tilbake på.

```bash
dotnet run
```

Terminalen viser adressen applikasjonen kjører på.

Eksempel:

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

## Prosjektstruktur

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
2. `KommandobroController` henter alle ressurser som har en registrert posisjon, og alle behov fra databasen.
3. Punktene sendes til viewet som JSON og tegnes på kartet med Leaflet.
4. Ressurser vises som grønne punkter, behov som røde punkter.
5. Brukeren kan klikke på et punkt for å se hva det gjelder.

Når en ny ressurs opprettes (`Views/Ressurs/Create.cshtml`), kan brukeren i tillegg velge posisjonen sin direkte på et interaktivt kart – et klikk plasserer en markør og fyller ut breddegrad/lengdegrad i skjemaet, som deretter lagres sammen med resten av ressursen.

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

- ChatGPT og Claude 

### Bruksområder

KI har blant annet blitt brukt til:

- Forklaring av ASP.NET Core MVC.
- Forståelse av Model, View og Controller.
- Veiledning ved implementering av Leaflet.
- Henting av latitude og longitude fra kartet.
- HTTP GET og POST.
- Overføring av data med ViewModel.
- Feilsøking av model binding.
- Feilsøking av koordinater som ble vist som 0.
- Navigasjon med `_Layout.cshtml`.
- Git og feature branches.
- Dokumentasjon av funksjonalitet.
- Utforming av testscenarioer.
- Oppsett av Dockerfile, .dockerignore og docker-compose.yml.
- Forklaring av hva hvert steg i Dockerfilen gjør.
- Oppsett av GitHub-repo, branches og pull requests.
- Feilsøking når kloning feilet på grunn av kolon i et filnavn.
- Feilsøking av tegnkoding (æ, ø og å) i filer laget med PowerShell.
- Feilsøking av byggefeil i Docker etter at prosjektet ble omdøpt.
- Feilsøking av manglende CSS i Docker fordi DesignCSS ikke ble med i publish.
- Struktur og drift-del i README.
- Oppsett av MariaDB i Docker.
- Valg og installasjon av riktige .NET- og Entity Framework-pakker.
- Tilkobling av databasen til ASP.NET Core-applikasjonen.
- Forståelse av hvordan database, Entity Framework og applikasjonen henger sammen.
- Feilsøking av feilmeldinger knyttet til database og pakker.
- Navigering i GitHub.

### Eksempler på prompts

Under utviklingen ble KI blant annet spurt om:

- "Hvordan lager jeg et interaktivt Leaflet-kart i ASP.NET Core MVC?"
- "Hvordan henter jeg latitude og longitude når brukeren klikker på et Leaflet-kart?"
- "Hvordan plasserer jeg en markør der brukeren klikker?"
- "Hvordan sender jeg koordinater fra en Razor View til en Controller med HTTP POST?"
- "Hvordan bruker jeg en ViewModel til å sende koordinater mellom View og Controller?"
- "Hvordan viser jeg koordinatene på en annen Razor View?"
- "Hvorfor blir Latitude og Longitude 0 etter POST i ASP.NET Core MVC?"
- "Hvordan kan jeg kontrollere Form Data i Network-verktøyet i nettleseren?"
- "Hvordan legger jeg MapController inn i navigasjonen i _Layout.cshtml?"
- "Hvordan bruker jeg en feature branch i Git uten å påvirke main?"
- "Hvordan dokumenterer jeg testscenarioer og resultater i README?"
- "Hva gjør hver linje i denne Dockerfilen?"
- "Hvorfor feiler git clone med 'invalid path' på Windows?"
- "Hvorfor vises æ, ø og å feil på GitHub etter at jeg lagret filen i PowerShell?"
- "Hvorfor mangler CSS når appen kjører i Docker, men ikke med dotnet run?"
- "Ett steg om gangen."

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
