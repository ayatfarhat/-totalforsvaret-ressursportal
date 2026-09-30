# Totalforsvaret Ressursportal

IS-200 / IS-201 / IS-202 – Kriseberedskap, ressurs- og behovsportal for Totalforsvaret.

## Om prosjektet

Nabohjelp er en ASP.NET Core MVC-applikasjon utviklet som et gruppeprosjekt.

Formålet med løsningen er å utvikle en webapplikasjon knyttet til kriseberedskap, ressurser og behov i Totalforsvaret.

## Teknologi

Prosjektet bruker:

- ASP.NET Core MVC
- .NET 10
- C#
- Razor
- HTML
- CSS
- JavaScript
- Bootstrap
- Leaflet
- OpenStreetMap
- Git og GitHub

## Systemarkitektur

Prosjektet følger MVC-arkitekturen:

### Model

Models brukes til å representere og overføre data i applikasjonen.

Kartfunksjonen bruker:

`Models/MapViewModel.cs`

Denne inneholder:

- Latitude
- Longitude

Koordinatene brukes til å overføre valgt posisjon fra kartet til controlleren og videre til resultatsiden.

### View

Views brukes til å vise brukergrensesnittet og data til brukeren.

Kartfunksjonen bruker:

- `Views/Map/Index.cshtml`
- `Views/Map/Result.cshtml`

`Index.cshtml` viser det interaktive kartet og lar brukeren velge en posisjon.

`Result.cshtml` viser koordinatene som brukeren har valgt.

### Controller

Controllers håndterer forespørsler mellom View og Model.

Kartfunksjonen bruker:

`Controllers/MapController.cs`

Controlleren inneholder:

- HTTP GET for å vise kartet.
- HTTP POST for å motta valgt posisjon.

## Kartfunksjonalitet

Kartfunksjonen lar brukeren velge en geografisk posisjon på et interaktivt kart.

Kartet er laget med Leaflet og bruker kartdata fra OpenStreetMap.

### Hvordan kartfunksjonen fungerer

1. Brukeren åpner kartsiden.
2. Et interaktivt kart vises.
3. Brukeren klikker på ønsket posisjon.
4. En markør plasseres på kartet.
5. Breddegrad og lengdegrad vises.
6. Koordinatene lagres i skjulte input-felter.
7. Knappen "Bekreft posisjon" aktiveres.
8. Brukeren bekrefter posisjonen.
9. Koordinatene sendes med HTTP POST til `MapController`.
10. Dataene mottas gjennom `MapViewModel`.
11. `Result.cshtml` viser den valgte breddegraden og lengdegraden.

## GET og POST

Applikasjonen håndterer både GET- og POST-forespørsler.

### GET

Når brukeren åpner kartsiden, brukes en GET-forespørsel for å vise kartet.

### POST

Når brukeren har valgt en posisjon og trykker på "Bekreft posisjon", sendes koordinatene med en POST-forespørsel til `MapController`.

Controlleren mottar koordinatene gjennom `MapViewModel` og sender modellen videre til resultatsiden.

## Responsivt design

Applikasjonen bruker Bootstrap for responsivt design.

Navigasjonsmenyen tilpasser seg størrelsen på skjermen. På mindre skjermer vises navigasjonen som en mobilmeny.

Kartet bruker hele den tilgjengelige bredden på siden slik at det tilpasser seg forskjellige skjermstørrelser.

## Testing

Kartfunksjonen er testet manuelt.

### Test 1 – Kartet vises

**Handling:**  
Brukeren åpner kartsiden.

**Forventet resultat:**  
Leaflet-kartet skal vises.

**Resultat:**  
Bestått.

### Test 2 – Velge posisjon

**Handling:**  
Brukeren klikker på kartet.

**Forventet resultat:**  
En markør skal vises på valgt posisjon, og breddegrad og lengdegrad skal vises.

**Resultat:**  
Bestått.

### Test 3 – Flytte valgt posisjon

**Handling:**  
Brukeren klikker på en ny posisjon på kartet.

**Forventet resultat:**  
Markøren skal flyttes til den nye posisjonen, og koordinatene skal oppdateres.

**Resultat:**  
Bestått.

### Test 4 – Sende koordinater

**Handling:**  
Brukeren velger en posisjon og trykker på "Bekreft posisjon".

**Forventet resultat:**  
Koordinatene skal sendes med HTTP POST til `MapController`.

**Resultat:**  
Bestått.

### Test 5 – Vise valgt posisjon

**Handling:**  
Brukeren bekrefter valgt posisjon.

**Forventet resultat:**  
Resultatsiden skal vise samme breddegrad og lengdegrad som brukeren valgte på kartet.

**Resultat:**  
Bestått.

### Test 6 – Tilbake til kartet

**Handling:**  
Brukeren trykker på "Tilbake til kartet".

**Forventet resultat:**  
Brukeren skal returnere til kartsiden.

**Resultat:**  
Bestått.

### Test 7 – Responsiv navigasjon

**Handling:**  
Applikasjonen åpnes i et smalt nettleservindu.

**Forventet resultat:**  
Navigasjonen skal endres til en mobilmeny, og kartlenken skal fortsatt være tilgjengelig.

**Resultat:**  
Bestått.

## Drift og kjøring

Prosjektet kan startes lokalt fra terminalen med:

```bash
dotnet run
```

Terminalen viser adressen applikasjonen kjører på.

Eksempel:

```text
http://localhost:5144
```

Kartfunksjonen kan åpnes fra navigasjonsmenyen ved å velge **Kart**.

### Docker

Den ferdige applikasjonen skal kjøres i Docker.

Docker-konfigurasjonen dokumenteres her når gruppens Docker-oppsett er ferdig og testet.

## Git og GitHub

Utviklingen gjøres med Git og GitHub.

Funksjonalitet utvikles på egne feature branches før den integreres med resten av prosjektet.

Kartfunksjonaliteten er utviklet på:

```text
feature/map
```

Dette gjør det mulig å utvikle og teste kartfunksjonen uten å påvirke hovedbranchen direkte.

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