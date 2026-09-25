# Totalforsvaret Ressursportal

IS-200 / IS-201 / IS-202 – Kriseberedskap, ressurs- og behovsportal for Totalforsvaret.

## Om prosjektet

Kriseportal er en ASP.NET Core MVC-applikasjon utviklet som et gruppeprosjekt.

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

## Arkitektur

Prosjektet følger MVC-arkitekturen:

### Model

Models brukes til å representere og overføre data i applikasjonen.

For kartfunksjonen brukes:

`MapViewModel.cs`

Denne inneholder:

- Latitude
- Longitude

### View

Views brukes til å vise brukergrensesnittet.

Kartfunksjonen bruker:

- `Views/Map/Index.cshtml`
- `Views/Map/Result.cshtml`

`Index.cshtml` viser kartet og lar brukeren velge en posisjon.

`Result.cshtml` viser koordinatene som brukeren har valgt.

### Controller

Controllers håndterer forespørsler mellom View og Model.

Kartfunksjonen bruker:

`MapController.cs`

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

## Kjøring av prosjektet

Prosjektet kan startes fra terminalen med:

```bash
dotnet run