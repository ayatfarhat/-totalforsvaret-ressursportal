# Totalforsvaret Ressursportal

IS-200 / IS-201 / IS-202 – Kriseberedskap, ressurs- og behovsportal for Totalforsvaret.

## Om Prosjektet

BeredskapsPortalen (tidligere Nabohjelp) er en ASP.NET Core MVC-applikasjon utviklet som et gruppeprosjekt.

Formålet med løsningen er å utvikle en webapplikasjon knyttet til kriseberedskap, ressurser og behov i Totalforsvaret.

### Hovedfunksjoner

- **Ressurser:** frivillige registrerer utstyr, kjøretøy, lokaler eller innsats med type, kommune, adresse, posisjon på kart og bilde. Eieren (og Kommune/Admin) kan endre og slette ressursen. En ressurs som er eller har vært tildelt, kan ikke slettes.
- **Behov:** innbyggere melder behov med type, hast, posisjon og kontaktinfo. Beskrivelsen får forslag etter type. Behovet kan endres og trekkes tilbake så lenge det ikke er tildelt, og statusen følges under «Mine behov».
- **Behovskø og statusflyt:** Kommune og Admin ser akutte behov først og flytter dem gjennom Ny → Under vurdering → Tildelt → Løst (eller Avvist). Reglene ligger i `Models/BehovStatusflyt.cs`.
- **Tildeling og matching:** løsningen foreslår ressurser som passer til behovstypen. Når et behov markeres som løst, blir ressursene ledige igjen.
- **Ressursliste per kommune:** Kommune og Admin kan filtrere på fylke eller kommune og skrive ut listen (eller lagre som PDF).
- **Ønskede ressurser:** kriseledelsen legger ut hva de trenger, og frivillige kan registrere en ressurs rett fra ønsket.
- **Kommandobro:** kart og tall over aktive behov og ressurser for kriseledelsen.

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

Begge bruker den samme MariaDB-databasen for brukere, behov, ressurser, tildelinger og ønskede ressurser. Bilder av ressurser lagres som filer i `wwwroot/uploads/ressurser/`. Kjør bare én av kjøremåtene om gangen.

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

Når appen kjører i utviklingsmodus (`Development`, som Docker og `dotnet run` bruker), opprettes tre testbrukere ved oppstart. De lages ikke i produksjon, fordi passordene står her:

| E-post | Passord | Rolle |
|---|---|---|
| admin@test.no | Admin123! | Admin |
| kommune@test.no | Kommune123! | Kommune |
| privatperson@test.no | Privat123! | Frivillig |
Tilgangsstyringen i appen bruker rollene `Admin`, `Kommune` og `Frivillig`.

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
| Innlogging og konto | `AccountController` | `Views/Account/` | `LoginViewModel`, `RegisterViewModel`, `KontoViewModel`, `TofaViewModel`, `EndrePassordViewModel`, `SlettKontoViewModel` |
| Brukeradministrasjon | `AdminController` | `Views/Admin/` | `AdminBrukerViewModel` |
| Ressurser (registrere, endre, slette, utskrift) | `RessursController` | `Views/Ressurs/` | `Ressurs`, `RessursFormViewModel`, `RessursUtskriftViewModel`, `Kommuner` |
| Ønskede ressurser | `OnsketRessursController` | `Views/OnsketRessurs/` | `OnsketRessurs` |
| Behov (melde, endre, trekke tilbake, kø) | `BehovController` | `Views/Behov/` | `Behov`, `BehovViewModel`, `BehovStatusflyt` |
| Tildeling og matching | `TildelingController` | `Views/Tildeling/` | `TildelingViewModel` |
| Kommandobro og kart | `KommandobroController` | `Views/Kommandobro/` | `MapViewModel`, `KartPunkt` |
| Forside, personvern og språkvalg | `HomeController` | `Views/Home/` | – |

Database-tilgangen går gjennom `ApplicationDbContext` i `Data/`, sammen med `SeedData.cs` (testbrukere) og `RoleInitializer.cs` (roller). Selve databasestrukturen styres av migrasjonsfilene i `Migrations/`.

Kode som brukes av flere controllere, ligger i `Services/`:

| Fil | Hva den gjør |
|---|---|
| `Tilgang.cs` | Én regel for hvem som er ansatt (Admin/Kommune, med 2FA når det kreves). Brukes av policyene, controllerne og sidene. |
| `Avstandsberegning.cs` | Luftlinjeavstand mellom behov og ressurs (Haversine-formelen). |
| `KontoService.cs` | Sletter en konto med data, og lager «Last ned mine data» (GDPR). |
| `PersonvernOpprydding.cs` | Fjerner kontaktinfo fra gamle, avsluttede behov ved oppstart (GDPR). |

### Filene på toppnivå

| Fil/mappe | Hvorfor den trengs |
|---|---|
| `.github/` | GitHub Actions bygger og tester koden ved hver pull request. |
| `.dockerignore` | Hindrer at `bin/`, `obj/` og opplastede bilder kopieres inn i Docker-bildet. |
| `.gitignore` | Hindrer at byggefiler og lokale innstillinger havner på GitHub. |
| `Controllers/`, `Models/`, `ViewModels/`, `Views/` | MVC-delene av appen. |
| `Data/`, `Migrations/` | Databasen: oppsett, testbrukere, roller og migrasjoner. |
| `Services/` | Kode som brukes av flere controllere (se over). |
| `Resources/` | Engelske tekster for språkvalget. |
| `SharedResource.cs` | Tom klasse som gir oversettelsene et navn. Må ligge utenfor `Resources/`, ellers finner ikke appen de engelske tekstene. |
| `Nabohjelp.Tests/` | Enhetstestene. |
| `wwwroot/` | CSS, JavaScript, skrifter, ikoner og bibliotekene Bootstrap og jQuery (bare filene som brukes). |
| `Properties/launchSettings.json` | Port og miljø når appen startes med `dotnet run`. |
| `appsettings.json`, `appsettings.Development.json` | Innstillinger, og egne innstillinger under utvikling (for eksempel at 2FA ikke kreves). |
| `Dockerfile`, `docker-compose.yml` | Bygger og starter appen og MariaDB i Docker. |
| `Nabohjelp.csproj`, `Nabohjelp.slnx` | Prosjektfilen, og løsningsfilen som åpner appen og testene sammen i Visual Studio/Rider. |
| `Program.cs` | Starter appen og setter opp database, innlogging, roller og språk. |

## Kartfunksjonalitet

Kartet viser en oversikt over registrerte ressurser og aktive behov, direkte på Kommandobro-siden. Det oppdateres når siden lastes på nytt. Det er laget med Leaflet og bruker kartdata fra OpenStreetMap.

Kartet er ikke en egen side i navigasjonsmenyen – det er bare tilgjengelig for brukere med rollen `Admin` eller `Kommune`, på samme måte som resten av Kommandobro.

### Hvordan kartfunksjonen fungerer

1. Brukeren logger inn som `Admin` eller `Kommune` og åpner Kommandobro.
2. `KommandobroController` henter ressurser med registrert posisjon og aktive behov (ikke løst, avvist eller trukket tilbake) fra databasen.
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

## Innlogging, roller, personvern og språk

Ansvarlig: Hassan.

### Tilgang for de ulike brukerrollene

| Rolle | Hvem | Tilgang |
|---|---|---|
| Frivillig | Alle nye brukere | Melde behov, registrere ressurser, se og endre sine egne registreringer |
| Kommune | Beredskapsansatte | I tillegg: Kommandobro, behovskø, tildeling, ressursliste per kommune, ønskede ressurser |
| Admin | Systemansvarlig | I tillegg: brukeradministrasjon (`/Admin`) |

Hvem som regnes som ansatt er definert ett sted, i policyen `Ansatt` i `Program.cs`. Controllerne bruker `[Authorize(Policy = "Ansatt")]` i stedet for å gjenta rollene. Brukeradministrasjonen bruker policyen `Admin`.

### Autentisering for ulike brukergrupper

- **Frivillige:** e-post og passord. 2FA er frivillig, så terskelen for å hjelpe blir lav.
- **Kommune og Admin:** ser personopplysninger om andre, så de kan kreves å logge inn med 2FA. Det styres med `Sikkerhet:KrevTofaForAnsatte` i `appsettings.json`. Innstillingen er `true` i produksjon og `false` i utvikling (`appsettings.Development.json`), så testbrukerne kan brukes uten autentiseringsapp. Når kravet er på, sjekker policyen at innloggingen har merket `amr = mfa`, som Identity legger til når brukeren har brukt 2FA.
- **Felles for alle:** passord lagres som hash, kontoen låses i 5 minutter etter 5 feil passord, innloggings-cookien er `HttpOnly`, og alle skjemaer har `[ValidateAntiForgeryToken]`.
- Rolleendringer og låsing gjelder innen ett minutt, fordi innloggingen sjekkes mot databasen hvert minutt (`SecurityStampValidatorOptions` i `Program.cs`).

### Administrasjon av brukerkontoer

**Admin (`/Admin`, menyvalget «Brukere»):**
- Se alle brukere med rolle, 2FA-status og om kontoen er låst. Søk på e-post.
- Endre rolle (Frivillig, Kommune eller Admin).
- Låse og låse opp kontoer.
- Nullstille 2FA for brukere som har mistet telefonen.
- Slette brukere med tilhørende behov og ressurser.

Admin kan ikke endre, låse eller slette sin egen konto fra listen, så den siste administratoren ikke kan låse seg selv ute.

**Alle brukere (Min konto):**
- Endre passord (krever nåværende passord).
- Slå 2FA på og av.
- Laste ned egne data og slette kontoen.

### Personvern og GDPR

| Krav i GDPR | Slik er det løst |
|---|---|
| Informasjon om behandlingen | Personvernerklæring på `/Home/Personvern`, lenket fra bunnen av alle sider |
| Samtykke | Avkrysning for personvernerklæringen ved registrering |
| Rett til innsyn og dataportabilitet | «Last ned mine data» på Min konto gir en JSON-fil med alt som er lagret om brukeren |
| Rett til sletting | «Slett konto» sletter kontoen, behovene, ressursene og bildene |
| Lagringsbegrensning | Telefon og e-post på behov som er løst, avvist eller trukket tilbake, slettes automatisk etter 90 dager (`Services/PersonvernOpprydding.cs`, kjøres ved oppstart). Antall dager settes i `Personvern:SlettKontaktinfoEtterDager` |
| Tilgangskontroll | Bare eieren, Kommune og Admin ser kontaktinformasjon |
| Dataminimering | Kontoen lagrer bare e-post, passord-hash, rolle og 2FA-nøkkel. Bare nødvendige cookies brukes |

Sletting av en bruker skjer i `Services/KontoService.cs`, som brukes både når brukeren sletter seg selv og når admin sletter en bruker. Tildelinger er historikk for kriseledelsen, så en konto med tildelinger må håndteres manuelt av administrator.

### Språkvalg (norsk og engelsk)

- Knappene **NO / EN** i menyen bytter språk. Valget lagres i en cookie i ett år.
- Tekstene skrives på norsk i koden som `@T["Logg inn"]`. Den engelske teksten ligger i `Resources/SharedResource.en.resx`. Mangler en oversettelse, vises den norske teksten.
- Bare språket på teksten endres. Tall leses fortsatt med punktum, så koordinatene fra kartet virker på begge språk.
- Oversatt: meny, bunntekst, forsiden, alle sider for innlogging og konto, brukeradministrasjon, personvern og menyen i Kommandobro. Andre sider oversettes på samme måte ved å legge teksten i `T["..."]` og i resx-filen.

### Testing av innlogging, roller, personvern og språk

Enhetstester: `Nabohjelp.Tests/Personvern_Tests.cs` tester regelen for automatisk sletting av kontaktinformasjon og passordreglene ved bytte av passord. Kjør med `dotnet test Nabohjelp.Tests`.

| # | Scenario | Forventet resultat | Resultat |
|---|---|---|---|
| H1 | Registrer uten å krysse av for personvern | Feilmelding, ingen konto lages | OK. «Du må godta personvernerklæringen …». Med avkrysning lages kontoen som Frivillig |
| H2 | Frivillig åpner `/Admin` og `/Kommandobro` | «Ingen tilgang» | OK. Sendes til «Ingen tilgang» for begge |
| H3 | Admin endrer rolle for en bruker til Kommune | Brukeren får tilgang til Kommandobro innen ett minutt | OK. Kommune-brukeren kom inn på Kommandobro, men ikke på `/Admin`. Ugyldig rolle gir 400 |
| H4 | Admin låser en bruker | Brukeren kan ikke logge inn. Står som «Låst» i listen | OK. «Låst» i listen og innlogging avvises. Lås opp virker. Fem feil passord låser også kontoen |
| H5 | Admin prøver å endre sin egen konto | Ikke mulig, ingen knapper på egen rad | OK. Ingen knapper på egen rad |
| H6 | Bruker endrer passord med feil nåværende passord | «Feil passord.» | OK. «Feil passord.» |
| H7 | Bruker endrer passord riktig | «Passordet er endret.» og nytt passord virker | OK. Innlogging med nytt passord virker |
| H8 | «Last ned mine data» | JSON-fil med konto, behov og ressurser | OK. `mine-data-beredskapsportalen.json` med konto, behov og ressurser |
| H9 | Admin sletter en bruker uten tildelinger | Brukeren og registreringene er borte | OK. Brukeren er slettet og kan ikke logge inn |
| H10 | Bytt til EN | Menyen, forsiden og kontosidene vises på engelsk. Valget huskes | OK. Meny, forside, innlogging, registrering, Min konto, Brukere og personvern på engelsk. Valget huskes |
| H11 | Registrer en ressurs med kartet på engelsk | Koordinatene lagres riktig | OK. Lagret som 58.1467 / 7.9956 |
| H12 | `KrevTofaForAnsatte = true`, Kommune uten 2FA åpner Kommandobro | Forklaring om at 2FA kreves, med lenke til Min konto | Ikke testet ennå (kravet er av i Development) |

## Testing

### Automatiske tester

Enhetstestene ligger i `Nabohjelp.Tests/` (xUnit) og dekker blant annet statusflyten for behov, behovsskjemaet, telefon med landskode, ønskede ressurser, kommunelisten og ressurslisten per kommune. De kjøres med:

```bash
dotnet test Nabohjelp.Tests/Nabohjelp.Tests.csproj
```

GitHub Actions (`.github/workflows/ci.yml`) bygger prosjektet og kjører testene ved hver pull request og push mot `main`.

### Manuelle tester av kartet

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
Bestått (håndheves av `[Authorize(Policy = "Ansatt")]` på `KommandobroController`, som krever rollen Admin eller Kommune).

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
- Brukeradministrasjon, personvern (GDPR), språkvalg og ulike krav til innlogging for frivillige og ansatte.

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
- "Hvordan lager jeg en admin-side der admin kan endre roller, låse og slette brukere i ASP.NET Core Identity?"
- "Hva krever GDPR av en app som lagrer telefon og e-post, og hvordan kan brukeren laste ned og slette sine egne data?"
- "Hvordan legger jeg til språkvalg mellom norsk og engelsk uten at desimaltall fra kartet slutter å virke?"

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
