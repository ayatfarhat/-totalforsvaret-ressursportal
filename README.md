# Totalforsvaret Ressursportal

IS-200 / IS-201 / IS-202 – Kriseberedskap, ressurs- og behovsportal for Totalforsvaret.

## Om Prosjektet

BeredskapsPortalen (tidligere Nabohjelp) er en ASP.NET Core MVC-applikasjon utviklet som et gruppeprosjekt.

Formålet med løsningen er å utvikle en webapplikasjon knyttet til kriseberedskap, ressurser og behov i Totalforsvaret.

### Hovedfunksjoner

- **Ressurser:** frivillige registrerer utstyr, kjøretøy, lokaler eller innsats med type, kommune, adresse, posisjon på kart og bilde. Det viktigste er påkrevd: type, kommune, adresse, når ressursen er tilgjengelig, beskrivelse, og navn og telefon til en kontaktperson. Eieren (og Kommune/Admin) kan endre og slette ressursen. En ressurs som er eller har vært tildelt, kan ikke slettes.
- **Behov:** innbyggere melder behov med type, hast, posisjon, kommune og kontaktinfo. Beskrivelsen får forslag etter type. Behovet kan endres og trekkes tilbake så lenge det ikke er tildelt, og statusen følges under «Mine behov».
- **Behovskø og statusflyt:** Kommune og Admin ser alle behov med posisjon (kommune og punkt på kart), nyeste først, og flytter dem gjennom Ny → Under vurdering → Tildelt → Løst (eller Avvist). Køen er delt i fanene Åpne, Pågår og Lukket, og kan filtreres på type, hast og kommune og sorteres med akutte først. Reglene ligger i `Models/BehovStatusflyt.cs`.
- **Ressursliste** (`/Ressurs/Oversikt`): Kommune og Admin ser alle ressursene, delt i Tilgjengelige og Tildelte, med filter på type og kommune.
- **Kommune fra kartet:** når brukeren trykker «Bruk min posisjon» eller på kartet, slår siden opp kommunen og nærmeste adresse hos Kartverket og fyller dem inn. Kommunen kan også skrives inn selv (med forslag mens man skriver).
- **Menyer:** innloggede har en brukermeny (Min konto, Mine behov, Mine ressurser, Logg ut). Kommune og Admin har i tillegg menyen «Kriseledelse» med behovskø, ressursliste, tildeling og Kommandobro.
- **Tildeling og matching:** løsningen foreslår ressurser som passer til behovstypen. Når et behov markeres som løst, blir ressursene ledige igjen.
- **Ressursliste per kommune:** Kommune og Admin kan filtrere på fylke eller kommune og skrive ut listen (eller lagre som PDF).
- **Ønskede ressurser:** kriseledelsen legger ut hva de trenger, og frivillige kan registrere en ressurs rett fra ønsket.
- **Kommandobro:** kart og tall over aktive behov og ressurser for kriseledelsen.
- **«Annet» med egen tekst:** finnes ikke typen i lista, velger brukeren «Annet» og skriver hva det er. Gjelder behov, ressurser og ønskede ressurser.
- **Søk** (`/Sok`): alle kan søke i sidene og i «Spørsmål og svar». Innloggede får også treff i behov og ressurser: frivillige i sine egne, Kommune og Admin i alle.
- **Infosider:** spørsmål og svar, personvernerklæring, vilkår for bruk og cookies, med «Sist oppdatert»-dato. En egen side for «Siden finnes ikke» (404) har søk og lenker videre.
- **Nødnumre:** 110, 112 og 113 øverst på alle sider er knapper som ringer nummeret på mobil.

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

Krypteringsnøklene ligger i volumet `nabohjelp_nokler`, adskilt fra databasen (se «Kryptering av personopplysninger»).

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
- Kartverkets åpne API-er (kommune og adresse fra et punkt på kartet)
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
| Ressurser (registrere, endre, slette, ressursliste, utskrift) | `RessursController` | `Views/Ressurs/` | `Ressurs`, `RessursFormViewModel`, `RessursListeViewModel`, `RessursUtskriftViewModel`, `Kommuner` |
| Ønskede ressurser | `OnsketRessursController` | `Views/OnsketRessurs/` | `OnsketRessurs` |
| Behov (melde, endre, trekke tilbake, kø med faner og filter) | `BehovController` | `Views/Behov/` | `Behov`, `BehovViewModel`, `BehovListeViewModel`, `BehovStatusflyt` |
| Tildeling og matching | `TildelingController` | `Views/Tildeling/` | `TildelingViewModel` |
| Kommandobro og kart | `KommandobroController` | `Views/Kommandobro/` | `MapViewModel`, `KartPunkt` |
| Forside, infosider (personvern, vilkår, cookies, spørsmål og svar), feilsider og språkvalg | `HomeController` | `Views/Home/` | `VanligeSporsmal` |
| Søk på nettsiden | `SokController` | `Views/Sok/` | `SokViewModel` |

`Models/` inneholder det som lagres i databasen (`Behov`, `Ressurs`, `Tildeling`, `OnsketRessurs`) og regler som hører til dem (`BehovStatusflyt`, `Kommuner`, `Visningstekst`). `ViewModels/` inneholder klassene som bare brukes til å vise sider og ta imot skjemaer, slik at brukeren aldri sender inn felt som eier, status eller ID direkte til databasen.

Database-tilgangen går gjennom `ApplicationDbContext` i `Data/`, sammen med `SeedData.cs` (testbrukere) og `RoleInitializer.cs` (roller). Selve databasestrukturen styres av migrasjonsfilene i `Migrations/`.

Kode som brukes av flere controllere, ligger i `Services/`:

| Fil | Hva den gjør |
|---|---|
| `Tilgang.cs` | Én regel for hvem som er ansatt (Admin/Kommune, med 2FA når det kreves). Brukes av policyene, controllerne og sidene. |
| `Avstandsberegning.cs` | Luftlinjeavstand mellom behov og ressurs (Haversine-formelen). |
| `KontoService.cs` | Sletter en konto med data, og lager «Last ned mine data» (GDPR). |
| `Ressursbilder.cs` | Sjekker, lagrer, finner og sletter bildene til ressursene (`wwwroot/uploads/ressurser`). Brukes av RessursController og KontoService. |
| `PersonvernOpprydding.cs` | Fjerner kontaktinfo fra gamle, avsluttede behov og sletter gamle invitasjonslenker ved oppstart (GDPR). |
| `Nettstedsok.cs` | Søket på nettsiden: sidene, spørsmål og svar, behov og ressurser. |
| `Nettsted.cs` | Datoen nettsiden sist ble oppdatert (vises nederst), og navnet på cookien for cookie-informasjonen. |
| `Ressursmatching.cs` | Hvilke ressurstyper som passer til hvilke behov, og nærmeste ledige ressurs. Brukes av tildelingen og Kommandobro-kartet. |
| `Invitasjonsregler.cs` | Lager og sjekker invitasjonslenker som gir rollen Kommune eller Admin. |
| `Sikkerhet.cs` | Sikkerhetshoder, fartsgrenser og beskyttelse mot søppelpost (se «Sikkerhetstiltak» under). |
| `Oversetter.cs` | Gir tekst på valgt språk i kode som ikke har `T["..."]`, for eksempel modeller og tjenester. |

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
4. Fargen viser hva punktet er: rødt er et akutt behov, gult et planlagt behov, grønt en ledig ressurs og grått en ressurs som er tildelt.
5. Et klikk på et punkt åpner en boks med alt om det. Øverst står typen og fargede merker for prioritet og status, og under det når behovet ble meldt (eller kommune og adresse for en ressurs). Beskrivelsen står i en egen grå boks, og telefon og e-post er lenker man kan ringe eller skrive til.
   - **Behov:** nederst står nærmeste ledige ressurs som passer, med avstand i luftlinje (`Services/Ressursmatching.cs`), i en grønn boks. Knappene «Finn ressurs» og «Endre» går rett til tildeling og redigering.
   - **Ressurs:** tilgjengelig fra, navn og telefon, med knappen «Endre».
6. Avstandsfilteret over kartet viser bare punktene innenfor valgt avstand fra et sted man klikker på.

Når en ny ressurs opprettes (`Views/Ressurs/Create.cshtml`), kan brukeren i tillegg velge posisjonen sin direkte på et interaktivt kart – et klikk plasserer en markør og fyller ut breddegrad/lengdegrad i skjemaet, som deretter lagres sammen med resten av ressursen.

## GET og POST

Applikasjonen håndterer både GET- og POST-forespørsler. Et eksempel er registrering av ressurs i `RessursController`:

- **GET:** Skjemaet for ny ressurs vises, med et interaktivt kart for å velge posisjon.
- **POST:** Når skjemaet sendes, mottas dataene og koordinatene gjennom `RessursFormViewModel`, og brukeren sendes videre til en bekreftelsesside som viser den valgte posisjonen på et kart.

## Responsivt design

Applikasjonen bruker Bootstrap for responsivt design.

Navigasjonsmenyen tilpasser seg størrelsen på skjermen. På mindre skjermer vises navigasjonen som en mobilmeny.

Kartet bruker hele den tilgjengelige bredden på kortet det står i, slik at det tilpasser seg forskjellige skjermstørrelser.

### Universell utforming (WCAG 2.1 AA)

- **Tastatur:** alt kan brukes med Tab, Enter og mellomrom. Den første lenken på hver side er «Hopp til innhold», og alt som får fokus, får en tydelig ramme. Spørsmål og svar bruker `<details>`, som virker med tastatur uten JavaScript.
- **Skjermleser:** valgt side i menyen har `aria-current="page"`, ikoner er skjult med `aria-hidden`, og nødnummer-knappene leses opp som «Ring 110, Brann».
- **Kontrast:** tekst har minst 4,5:1 mot bakgrunnen, og kanter rundt felt minst 3:1. Teksten i tomme felt (placeholder) er gjort mørkere for å nå kravet.
- **Sjekket med** [axe-core](https://github.com/dequelabs/axe-core) på alle sidene for alle roller (WCAG 2.1 A og AA): ingen feil.

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
- **Vipps og Apple:** knappene «Logg inn med Vipps» og «Logg inn med Apple» finnes på innloggings- og registreringssiden (se «Innlogging med Vipps og Apple» under).

### Sikkerhetstiltak

| Trussel | Tiltak | Hvor i koden |
|---|---|---|
| Gjetting av passord | Kontoen låses i 5 minutter etter 5 feil passord. Maks 10 forsøk per minutt per IP-adresse på innlogging, registrering og invitasjoner (feilkode 429) | `Program.cs`, `Services/Sikkerhet.cs` |
| Svake passord | Minst 8 tegn med stor og liten bokstav, tall og spesialtegn, både i skjemaene og i Identity | `RegisterViewModel`, `Program.cs` |
| Stjålet passord | 2FA med autentiseringsapp. Kan kreves for Kommune og Admin (`Sikkerhet:KrevTofaForAnsatte`) | `AccountController`, `Services/Tilgang.cs` |
| Tilgang til andres data | Policyene `Ansatt` og `Admin`, og sjekk av eier før endring og sletting | Controllerne, `Services/Tilgang.cs` |
| Falske skjemaer fra andre nettsider (CSRF) | `[ValidateAntiForgeryToken]` på alle POST-handlinger, og cookies med `SameSite` | Controllerne, `Program.cs` |
| Skript i tekst brukere skriver (XSS) | Razor gjør om all tekst til trygg HTML. Content-Security-Policy bestemmer hvor skript kan hentes fra | Sidene, `Services/Sikkerhet.cs` |
| Siden vist inne i en falsk nettside (clickjacking) | `X-Frame-Options: DENY` og `frame-ancestors 'none'` | `Services/Sikkerhet.cs` |
| Farlige filer lastet opp som bilder | Sjekk av filtype, størrelse (maks 5 MB) og de første bytene i filen. Filnavnet lages av appen | `Services/Ressursbilder.cs` |
| Videresending til falske nettsider etter innlogging | Bare adresser på egen side godtas (`Url.IsLocalUrl`) | `AccountController`, `HomeController` |
| Misbruk av invitasjonslenker | Tilfeldig kode på 32 byte, lagret som SHA-256, kan brukes én gang og går ut | `Services/Invitasjonsregler.cs` |
| Søppelpost fra roboter | Et skjult felt («honningkrukke») i registrering, behov og ressurser. Er det fylt ut, avvises skjemaet. Maks 20 nye behov eller ressurser per minutt for hver bruker | `Services/Sikkerhet.cs`, `Views/Shared/_Honningkrukke.cshtml` |
| Informasjon om serveren | Svarhodet `Server` er fjernet, og feilsider viser ingen detaljer i drift | `Program.cs` |
| Lekket database eller sikkerhetskopi | Telefon, e-post og navn på behov og ressurser lagres kryptert (AES-256). Nøklene ligger utenfor databasen | `Services/Kryptering.cs`, `Data/ApplicationDbContext.cs` |
| Passord på avveie hos oss | Med Vipps og Apple får brukeren ingen konto med passord hos oss | `Services/EksternInnlogging.cs` |

Tiltakene testes av enhetstestene og systemtestene (se «Testing»).

### Administrasjon av brukerkontoer

**Admin (`/Admin`, menyvalget «Brukere»):**
- Se alle brukere med rolle, 2FA-status og om kontoen er låst. Søk på e-post.
- Endre rolle (Frivillig, Kommune eller Admin).
- Låse og låse opp kontoer.
- Nullstille 2FA for brukere som har mistet telefonen.
- Slette brukere med tilhørende behov og ressurser.
- Lage invitasjonslenker (`/Admin/Invitasjoner`) som gir rollen Kommune eller Admin. Lenken gjelder i 1, 3 eller 7 dager og kan brukes én gang. Den som åpner lenken, logger inn eller lager konto og trykker «Ta imot rollen». Databasen lagrer bare et fingeravtrykk (SHA-256) av koden, så lenken vises bare én gang for admin.

Admin kan ikke endre, låse eller slette sin egen konto fra listen, så den siste administratoren ikke kan låse seg selv ute.

**Alle brukere (Min konto):**
- Endre passord (krever nåværende passord).
- Slå 2FA på og av.
- Laste ned egne data og slette kontoen.

### Personvern og GDPR

| Krav i GDPR | Slik er det løst |
|---|---|
| Informasjon om behandlingen | Personvernerklæring på `/Home/Personvern`, vilkår for bruk på `/Home/Vilkar` og cookie-erklæring på `/Home/Cookies`, lenket fra bunnen av alle sider |
| Samtykke | Avkrysning for vilkårene og personvernerklæringen ved registrering |
| Cookies | Bare nødvendige cookies, så samtykke trengs ikke (ekomloven § 3-15). En boks nederst forteller om dem til brukeren trykker «OK» (`Views/Shared/_CookieInfo.cshtml`) |
| Rett til innsyn og dataportabilitet | «Last ned mine data» på Min konto gir en JSON-fil med alt som er lagret om brukeren |
| Rett til sletting | «Slett konto» sletter kontoen, behovene, ressursene og bildene |
| Lagringsbegrensning | Telefon og e-post på behov som er løst, avvist eller trukket tilbake, slettes automatisk etter 90 dager (`Services/PersonvernOpprydding.cs`, kjøres ved oppstart). Antall dager settes i `Personvern:SlettKontaktinfoEtterDager`. Invitasjonslenker slettes like mange dager etter at de er brukt eller har gått ut, fordi de inneholder e-posten til den som brukte dem |
| Tilgangskontroll | Bare eieren, Kommune og Admin ser kontaktinformasjon |
| Dataminimering | Kontoen lagrer bare e-post, passord-hash, rolle og 2FA-nøkkel. På ressurser spør vi bare om det kriseledelsen trenger (kontaktperson og telefon). Bare nødvendige cookies brukes |
| Sikkerhet ved behandlingen (artikkel 32) | Personopplysninger på behov og ressurser lagres kryptert. Se «Kryptering av personopplysninger» under |

Sletting av en bruker skjer i `Services/KontoService.cs`, som brukes både når brukeren sletter seg selv og når admin sletter en bruker. Tildelinger er historikk for kriseledelsen, så en konto med tildelinger må håndteres manuelt av administrator.

### Innlogging med Vipps og Apple

Knappene ligger på innloggings- og registreringssiden (`Views/Shared/_EksternInnlogging.cshtml`). Koden for selve innloggingen er ferdig, men er **slått av** i denne versjonen:

- **Vipps Login** krever at man er en bedrift med avtale hos Vipps MobilePay (organisasjonsnummer) og et salgssted med «Logg inn». Først da får man nøkler, også til testmiljøet.
- **Sign in with Apple** krever medlemskap i Apple Developer Program og et ekte domene med https. Apple godtar ikke `localhost`.

Som studentgruppe har vi ingen av delene. Trykker man på en knapp nå, får man en melding om at innloggingen ikke er slått på ennå, og kan logge inn med e-post og passord.

**Slik virker det når nøklene er på plass** (OpenID Connect, `Services/EksternInnlogging.cs`):
1. Brukeren trykker «Logg inn med Vipps» og sendes til Vipps (`AccountController.LoggInnMed`).
2. Brukeren godkjenner i Vipps-appen. Vi ser aldri passordet.
3. Vipps sender brukeren tilbake til `/signin-vipps`. ASP.NET henter e-posten direkte fra Vipps.
4. `AccountController.LoggInnMedSvar` logger inn brukeren hvis hen har vært her før. Første gang lages en konto med rollen Frivillig, uten passord.
5. Har brukeren 2FA, må koden fortsatt skrives inn.

Finnes det allerede en konto med samme e-post, kobles den **ikke** automatisk. Ellers kunne noen som styrer en konto hos leverandøren, tatt over kontoen her. Vi ber bare om e-post (dataminimering).

**Slå på:** legg nøklene i `EksternInnlogging:Vipps` eller `EksternInnlogging:Apple`, helst som miljøvariabler eller user secrets, ikke i `appsettings.json` på GitHub:

```bash
EksternInnlogging__Vipps__ClientId=...
EksternInnlogging__Vipps__ClientSecret=...
```

- Vipps: velg `client_secret_post` som innloggingsmetode for salgsstedet, og legg inn `https://<domene>/signin-vipps` som tilbakeadresse.
- Apple: `ClientId` er Services ID. `ClientSecret` er et signert token (JWT) laget med nøkkelfilen (.p8), og må fornyes hver sjette måned. Tilbakeadressen er `https://<domene>/signin-apple`.
- Bytt knappene til de offisielle knappene og logoene fra Vipps og Apple. Begge har regler for hvordan knappene skal se ut.

### Kryptering av personopplysninger

Telefon og e-post på behov, og navn og telefon på ressurser, lagres kryptert i databasen. Ser man i tabellen, står det for eksempel `kryptert:CfDJ8N...` i stedet for telefonnummeret.

- **Hvordan:** ASP.NET Core Data Protection (AES-256-CBC med HMAC-SHA256). Den samme teksten gir ny kryptert tekst hver gang, så man kan ikke se hvem som har samme nummer. Endrer noen den krypterte teksten, oppdages det.
- **Hvor:** `HasConversion` i `Data/ApplicationDbContext.cs` sender feltene gjennom `Kryptering.Krypter` når de lagres og `Kryptering.Dekrypter` når de leses. Resten av koden ser vanlig tekst.
- **Nøklene** ligger i mappen `nokler`, som er et eget Docker-volum (`nabohjelp_nokler`), atskilt fra databasen. Mappen står i `.gitignore` og kommer aldri på GitHub. Uten volumet ville nye nøkler blitt laget hver gang containeren bygges, og ingen kunne lest opplysningene.
- **Gamle data:** verdier lagret før krypteringen fantes, krypteres automatisk når appen starter (`Kryptering.KrypterGamleOpplysninger`).
- **Hvis nøklene forsvinner**, vises «(kan ikke vises)» i stedet for at siden krasjer.
- **Passord** krypteres ikke, men lagres som hash (Identity). En hash kan ikke gjøres om til passordet igjen.
- **Ikke kryptert:** e-posten på brukerkontoen (Identity trenger den til innlogging og søk), beskrivelser og posisjon (kriseledelsen må kunne søke og vise dem på kartet).
- **I drift** bør nøklene i tillegg beskyttes med et sertifikat eller et nøkkelhvelv (for eksempel Azure Key Vault), og nettsiden kjøres med https (`UseHttpsRedirection` og HSTS er allerede på).

### Språkvalg (norsk og engelsk)

- Språkknappen øverst til høyre («English» / «Norsk») bytter språk. Valget lagres i en cookie i ett år.
- Tekstene skrives på norsk i koden som `@T["Logg inn"]`. Den engelske teksten ligger i `Resources/SharedResource.en.resx`. Mangler en oversettelse, vises den norske teksten.
- Bare språket på teksten endres. Tall leses fortsatt med punktum, så koordinatene fra kartet virker på begge språk.
- Alle sider er oversatt, også meldinger fra controllerne, feilmeldinger i skjemaene og boksene på kartet. Kode som ikke har `T` (modeller og tjenester), bruker `Oversetter.Tekst("...")`.
- Tekstene i JavaScript (`kart.js` og `ressurs-skjema.js`) hentes med `tekst('...')`. Listen over dem står i `Views/Shared/_JsTekster.cshtml`.
- Enhetstesten `AlleTeksterISidene_HarEngelskOversettelse` feiler hvis en ny tekst mangler i resx-filen.
- Nytt språk, for eksempel samisk: lag `Resources/SharedResource.se.resx` med de samme nøklene, legg `new CultureInfo("se")` til i `SupportedUICultures` i `Program.cs`, og legg til en knapp i `Views/Shared/_SprakValg.cshtml`.

### Testing av innlogging, roller, personvern og språk

Enhetstester: `Nabohjelp.Tests/Personvern_Tests.cs` tester regelen for automatisk sletting av kontaktinformasjon og passordreglene ved bytte av passord. `Nabohjelp.Tests/Kryptering_Tests.cs` tester krypteringen og at bare Vipps og Apple godtas. Kjør med `dotnet test Nabohjelp.Tests`.

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
| H13 | Trykk «Logg inn med Vipps» | Melding om at Vipps ikke er slått på ennå | OK. Meldingen vises på innloggingssiden. En ukjent leverandør (for eksempel «Google») gir 400 |
| H14 | Trykk «Logg inn med Apple» | Melding om at Apple ikke er slått på ennå | OK |
| H15 | Registrer en ressurs med navn og telefon, og se i databasen | Navn og telefon står som `kryptert:...` i tabellen `Ressurser`, men vises riktig i appen | OK. Begge felt er `kryptert:CfDJ8...` i databasen, også for en ressurs lagret før krypteringen fantes. Vises riktig på kvitteringen, Kommandobroen og i «Last ned mine data» |
| H16 | Bygg containeren på nytt (`docker compose up -d --build --force-recreate web`) | Navn og telefon vises fortsatt riktig (nøklene er beholdt) | OK |

## Testing

### Automatiske tester

Testene ligger i `Nabohjelp.Tests/` (xUnit) og er av to slag.

**Enhetstester** tester én klasse om gangen, uten database eller nettleser. De dekker blant annet statusflyten for behov, skjemaene, telefon med landskode, ønskede ressurser, kommunelisten, utskrift per kommune, tilgangsregelen, bilder, invitasjonslenker, matching av ressurser, nærmeste ressurs, fanene og filteret i behovskøen og ressurslisten, søket, beskyttelsen mot søppelpost, sletting av gamle invitasjoner og at alle tekster har engelsk oversettelse.

```bash
dotnet test Nabohjelp.Tests/Nabohjelp.Tests.csproj --filter "Kategori!=Systemtest&Kategori!=Fartsgrense"
```

**Systemtester** (`Systemtester.cs`) tester hele appen med ekte database, slik en bruker møter den. De logger inn som testbrukerne og sender de samme forespørslene som nettleseren:

| Test | Hva den sjekker |
|---|---|
| `Forsiden_HarSikkerhetshoder` | Sikkerhetshodene er med, og `Server`-hodet er fjernet |
| `IkkeInnlogget_SendesTilInnlogging` | Beskyttede sider krever innlogging |
| `Frivillig_FarIkkeTilgangTilKriseledelsensSider` | Frivillige kommer ikke inn på Kommandobro, behovskø, tildeling, utskrift og admin |
| `Kommune_FarIkkeTilgangTilBrukeradministrasjon` | Kommune kommer inn på Kommandobro, men ikke på admin |
| `Skjema_UtenAntiForgeryToken_Avvises` | Skjema uten token avvises (CSRF) |
| `Skjema_MedSkjultFeltUtfylt_AvvisesSomSoppelpost` | Et behov med det skjulte feltet utfylt (slik roboter gjør) avvises med 400 |
| `FeilPassord_GirFeilmeldingOgIngenInnlogging` | Feil passord gir feilmelding |
| `Hovedflyt_MeldeBehov_RegistrereRessurs_Tildele` | Frivillig melder behov og registrerer aggregat, kommunen tildeler, og statusen blir «Tildelt» |
| `Invitasjonslenke_GirRollenEnGang` | En ny bruker får rollen Kommune fra lenken, og lenken virker ikke en gang til |
| `Sprakvalg_ViserSidenPaEngelsk` | Språkknappen gir engelsk side |
| `MangeInnloggingsforsok_GirFeilkode429` | Fartsgrensen stopper forsøk nr. 11 innen ett minutt |

Systemtestene trenger at appen kjører. Lokalt:

```bash
docker compose up -d --build
SYSTEMTEST_URL=http://localhost:8080 dotnet test Nabohjelp.Tests/Nabohjelp.Tests.csproj --filter "Kategori=Systemtest"
```

Uten `SYSTEMTEST_URL` hopper systemtestene over seg selv.

GitHub Actions (`.github/workflows/ci.yml`) kjører enhetstestene ved hver pull request og push mot `main`. Jobben `systemtest` starter appen og MariaDB med Docker og kjører systemtestene, så den sjekker også at Docker-oppsettet og migrasjonene virker.

**Brukertest** med ekte personer kan ikke automatiseres. Planen med oppgaver, resultatskjema og SUS-skjema ligger i gruppens dokument «Brukertest – BeredskapsPortalen».

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
- Kryptering av personopplysninger i databasen, og innlogging med Vipps og Apple (OpenID Connect), inkludert hva som kreves for å få nøkler.

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
- "Er det mulig å lage innlogging med Vipps og Apple i et studentprosjekt, eller trenger man godkjenning?"
- "Hvordan krypterer jeg telefonnummer og e-post i databasen med Entity Framework, uten å endre resten av koden?"

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


### Brukervennlighetstest
Vi har gjennomført en innledende brukervennlighetstest av tidligere nabohjelp, nå BeredskapsPortalen for å undersøke hvor enkelt det er for en ny bruker å navigere applikasjonen og gjennomføre essensielle oppgaver.

Målet med testen var å identifisere eventuelle utfordringer i brukergrensesnittet og samle konkrete tilbakemeldinger som kan brukes til videreutvikling.

Dette er den første brukervennlighetstesten. Vi planlegger å teste systemet med flere ulike målgrupper etter hvert.

Testperson var en mann i 30 årene, testtypen var moderat brukervennlighetstest og det samledet tidsbruket for alle oppgavene var ca. 7 minutter.

Testpersonen fikk tre oppgaver som skulle gjennomføres i applikasjonen på en bærbar datamaskin. Oppgavene ble presentert uten en forhåndsforklaring.

Oppgave 1: Registrere en ny brukerkonto - Resultat: Fullført.

Oppgave 2: Strømmen er borte hjemme, og du trenger en aggregat. Registrer behovet. - Resultat: Fullført, men med utfordringer.

Oppgave 3: Du har en tilhenger du ønsker å låne bort. Registrer ressursen. -
Resultat: Fullført.

Samlet resultat: Testperson fullførte alle tre oppgaver på omtrent 7 miutter.

Vurdering av brukervennlighet:
Testperson ble bedt om å vurdere applikasjonen på en skala fra 1 til 5. (1= svært vanskelig, 5= svært enkelt)

Spørsmål:
Hvor enkelt oppleves systemet å bruke?
Svar: 4

Hvor mye hjelp ville du hatt behov for?
Svar: 1

Generelle spørsmål etter testen:
1. Hvilken oppgave var vanskeligst?

Svar: Legge inn ressurs var mest krevende, prosessen krevde mer informasjon og hadde flere steg, testperson fikk også en feilmelding grunnet tiden som ble lagt inn var i "fortid".

2. Var det noen knapper eller begreper du ikke forstod?

Svar: Ikke underveis i testen, men når testperson skulle se over sine registreringer, syntes vedkommende at det var vanskelig å skille mellom "Mine Behov" og "Mine Ressurser" og mente at de burde skilles mer tydelig med farge.

3. Var det tydelig når en registrering var fullført?

Svar: Ja, det var tydelig og vedkommende fikk en kvittering med oppsummering.

4. Hva ville du forbedret i applikasjonen?

Svar: En egen knapp på forsiden der mine registreringer står tydelig, da vedkommende brukt litt tid på å finne frem til den i etterkant av testen. Testperson mente også at kommandobro burde ha en oversikt over alle tilgjengelige ressurser på lik linje som registrerte behov.

Resultatet av denne brukervennlighetstesten er foreløpige og baser på kun én testperson. Flere tester med ulike målgrupper vil være nødvendig for å vurdere hvor representative funnene er.
