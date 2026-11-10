namespace Nabohjelp.Models;

// Ett spørsmål med svar på siden «Spørsmål og svar» (/Home/Sporsmal).
public class VanligSporsmal
{
    public string Sporsmal { get; set; } = "";

    public string Svar { get; set; } = "";
}

// Spørsmålene står på norsk her. Den engelske teksten ligger i
// Resources/SharedResource.en.resx, som for resten av sidene.
// Søket (Services/Nettstedsok.cs) leter også i denne lista.
public static class VanligeSporsmal
{
    public static readonly List<VanligSporsmal> Alle = new List<VanligSporsmal>
    {
        new VanligSporsmal
        {
            Sporsmal = "Hva er BeredskapsPortalen?",
            Svar = "En nettside der innbyggere kan melde behov for hjelp og registrere utstyr, kjøretøy eller innsats de kan stille med under en krise. Kommunen og kriseledelsen kobler behovene med ressursene."
        },
        new VanligSporsmal
        {
            Sporsmal = "Er dette en nødtjeneste?",
            Svar = "Nei. Er det akutt fare for liv og helse, ring 110 (brann), 112 (politi) eller 113 (ambulanse) først. Knappene øverst på siden ringer nummeret direkte fra mobilen."
        },
        new VanligSporsmal
        {
            Sporsmal = "Må jeg ha en konto?",
            Svar = "Du kan lese sidene uten konto. For å melde et behov eller registrere en ressurs må du lage en gratis konto med e-post og passord."
        },
        new VanligSporsmal
        {
            Sporsmal = "Hvem ser telefonnummeret og e-posten min?",
            Svar = "Bare du selv, og kommunen og kriseledelsen (rollene Kommune og Admin). Andre innbyggere ser ikke kontaktinformasjonen din."
        },
        new VanligSporsmal
        {
            Sporsmal = "Ressursen min finnes ikke i lista. Hva gjør jeg?",
            Svar = "Velg «Annet» og skriv hva det er, for eksempel «vannpumpe». Det samme gjelder når du melder et behov."
        },
        new VanligSporsmal
        {
            Sporsmal = "Hvordan endrer eller sletter jeg en ressurs?",
            Svar = "Gå til Ressurser. Der ser du ressursene du har registrert, med knappene Endre og Slett."
        },
        new VanligSporsmal
        {
            Sporsmal = "Hva betyr statusene på et behov?",
            Svar = "Ny: behovet er mottatt. Under vurdering: kommunen ser på det. Tildelt: en ressurs er sendt. Løst: behovet er dekket. Avvist eller Trukket tilbake: behovet følges ikke opp videre."
        },
        new VanligSporsmal
        {
            Sporsmal = "Kan jeg endre eller trekke tilbake et behov?",
            Svar = "Ja, under Mine behov, så lenge behovet har status Ny eller Under vurdering."
        },
        new VanligSporsmal
        {
            Sporsmal = "Hvordan gjør jeg kontoen min sikrere?",
            Svar = "Slå på tofaktor-innlogging under Min konto. Da trenger du en kode fra en autentiseringsapp i tillegg til passordet."
        },
        new VanligSporsmal
        {
            Sporsmal = "Hvordan ser eller sletter jeg opplysningene mine?",
            Svar = "Under Min konto kan du laste ned alt vi har lagret om deg, og slette kontoen. Les mer i personvernerklæringen."
        },
        new VanligSporsmal
        {
            Sporsmal = "Hvordan bytter jeg språk?",
            Svar = "Trykk på språkknappen helt til høyre i menyen. Valget huskes til neste gang."
        },
        new VanligSporsmal
        {
            Sporsmal = "Hvorfor ser jeg ikke kartet med alle behov?",
            Svar = "Kommandobroen med kartet er bare for kommunen og kriseledelsen. Har du fått en invitasjonslenke fra kommunen, åpner du den for å få tilgang."
        }
    };
}
