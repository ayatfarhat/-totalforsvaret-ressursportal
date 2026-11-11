namespace Nabohjelp.Models;

// Alle kommuner i Norge (357, inndelingen fra 2024), med kommunenummer og fylke.
// Brukes i nedtrekkslisten «Kommune» på ressursskjemaet og i utskriften per region.
// Kilde: postnummerregisteret til Bring (via npm-pakken «postnummer» 2.0.0).
// Kommunenummeret lagres i databasen; navnet slås opp her når det skal vises.
public class Kommune
{
    public string Nummer { get; }
    public string Navn { get; }
    public string Fylke { get; }

    public Kommune(string nummer, string navn, string fylke)
    {
        Nummer = nummer;
        Navn = navn;
        Fylke = fylke;
    }
}

public static class Kommuner
{
    public const string IkkeOppgitt = "Kommune ikke oppgitt";

    public static readonly List<Kommune> Alle = new List<Kommune>
    {
        new Kommune("0301", "Oslo", "Oslo"),
        new Kommune("1101", "Eigersund", "Rogaland"),
        new Kommune("1103", "Stavanger", "Rogaland"),
        new Kommune("1106", "Haugesund", "Rogaland"),
        new Kommune("1108", "Sandnes", "Rogaland"),
        new Kommune("1111", "Sokndal", "Rogaland"),
        new Kommune("1112", "Lund", "Rogaland"),
        new Kommune("1114", "Bjerkreim", "Rogaland"),
        new Kommune("1119", "Hå", "Rogaland"),
        new Kommune("1120", "Klepp", "Rogaland"),
        new Kommune("1121", "Time", "Rogaland"),
        new Kommune("1122", "Gjesdal", "Rogaland"),
        new Kommune("1124", "Sola", "Rogaland"),
        new Kommune("1127", "Randaberg", "Rogaland"),
        new Kommune("1130", "Strand", "Rogaland"),
        new Kommune("1133", "Hjelmeland", "Rogaland"),
        new Kommune("1134", "Suldal", "Rogaland"),
        new Kommune("1135", "Sauda", "Rogaland"),
        new Kommune("1144", "Kvitsøy", "Rogaland"),
        new Kommune("1145", "Bokn", "Rogaland"),
        new Kommune("1146", "Tysvær", "Rogaland"),
        new Kommune("1149", "Karmøy", "Rogaland"),
        new Kommune("1151", "Utsira", "Rogaland"),
        new Kommune("1160", "Vindafjord", "Rogaland"),
        new Kommune("1505", "Kristiansund", "Møre og Romsdal"),
        new Kommune("1506", "Molde", "Møre og Romsdal"),
        new Kommune("1508", "Ålesund", "Møre og Romsdal"),
        new Kommune("1511", "Vanylven", "Møre og Romsdal"),
        new Kommune("1514", "Sande", "Møre og Romsdal"),
        new Kommune("1515", "Herøy (Møre og Romsdal)", "Møre og Romsdal"),
        new Kommune("1516", "Ulstein", "Møre og Romsdal"),
        new Kommune("1517", "Hareid", "Møre og Romsdal"),
        new Kommune("1520", "Ørsta", "Møre og Romsdal"),
        new Kommune("1525", "Stranda", "Møre og Romsdal"),
        new Kommune("1528", "Sykkylven", "Møre og Romsdal"),
        new Kommune("1531", "Sula", "Møre og Romsdal"),
        new Kommune("1532", "Giske", "Møre og Romsdal"),
        new Kommune("1535", "Vestnes", "Møre og Romsdal"),
        new Kommune("1539", "Rauma", "Møre og Romsdal"),
        new Kommune("1547", "Aukra", "Møre og Romsdal"),
        new Kommune("1554", "Averøy", "Møre og Romsdal"),
        new Kommune("1557", "Gjemnes", "Møre og Romsdal"),
        new Kommune("1560", "Tingvoll", "Møre og Romsdal"),
        new Kommune("1563", "Sunndal", "Møre og Romsdal"),
        new Kommune("1566", "Surnadal", "Møre og Romsdal"),
        new Kommune("1573", "Smøla", "Møre og Romsdal"),
        new Kommune("1576", "Aure", "Møre og Romsdal"),
        new Kommune("1577", "Volda", "Møre og Romsdal"),
        new Kommune("1578", "Fjord", "Møre og Romsdal"),
        new Kommune("1579", "Hustadvika", "Møre og Romsdal"),
        new Kommune("1580", "Haram", "Møre og Romsdal"),
        new Kommune("1804", "Bodø", "Nordland"),
        new Kommune("1806", "Narvik", "Nordland"),
        new Kommune("1811", "Bindal", "Nordland"),
        new Kommune("1812", "Sømna", "Nordland"),
        new Kommune("1813", "Brønnøy", "Nordland"),
        new Kommune("1815", "Vega", "Nordland"),
        new Kommune("1816", "Vevelstad", "Nordland"),
        new Kommune("1818", "Herøy (Nordland)", "Nordland"),
        new Kommune("1820", "Alstahaug", "Nordland"),
        new Kommune("1822", "Leirfjord", "Nordland"),
        new Kommune("1824", "Vefsn", "Nordland"),
        new Kommune("1825", "Grane", "Nordland"),
        new Kommune("1826", "Hattfjelldal", "Nordland"),
        new Kommune("1827", "Dønna", "Nordland"),
        new Kommune("1828", "Nesna", "Nordland"),
        new Kommune("1832", "Hemnes", "Nordland"),
        new Kommune("1833", "Rana", "Nordland"),
        new Kommune("1834", "Lurøy", "Nordland"),
        new Kommune("1835", "Træna", "Nordland"),
        new Kommune("1836", "Rødøy", "Nordland"),
        new Kommune("1837", "Meløy", "Nordland"),
        new Kommune("1838", "Gildeskål", "Nordland"),
        new Kommune("1839", "Beiarn", "Nordland"),
        new Kommune("1840", "Saltdal", "Nordland"),
        new Kommune("1841", "Fauske", "Nordland"),
        new Kommune("1845", "Sørfold", "Nordland"),
        new Kommune("1848", "Steigen", "Nordland"),
        new Kommune("1851", "Lødingen", "Nordland"),
        new Kommune("1853", "Evenes", "Nordland"),
        new Kommune("1856", "Røst", "Nordland"),
        new Kommune("1857", "Værøy", "Nordland"),
        new Kommune("1859", "Flakstad", "Nordland"),
        new Kommune("1860", "Vestvågøy", "Nordland"),
        new Kommune("1865", "Vågan", "Nordland"),
        new Kommune("1866", "Hadsel", "Nordland"),
        new Kommune("1867", "Bø", "Nordland"),
        new Kommune("1868", "Øksnes", "Nordland"),
        new Kommune("1870", "Sortland", "Nordland"),
        new Kommune("1871", "Andøy", "Nordland"),
        new Kommune("1874", "Moskenes", "Nordland"),
        new Kommune("1875", "Hamarøy", "Nordland"),
        new Kommune("3101", "Halden", "Østfold"),
        new Kommune("3103", "Moss", "Østfold"),
        new Kommune("3105", "Sarpsborg", "Østfold"),
        new Kommune("3107", "Fredrikstad", "Østfold"),
        new Kommune("3110", "Hvaler", "Østfold"),
        new Kommune("3112", "Råde", "Østfold"),
        new Kommune("3114", "Våler (Østfold)", "Østfold"),
        new Kommune("3116", "Skiptvet", "Østfold"),
        new Kommune("3118", "Indre Østfold", "Østfold"),
        new Kommune("3120", "Rakkestad", "Østfold"),
        new Kommune("3122", "Marker", "Østfold"),
        new Kommune("3124", "Aremark", "Østfold"),
        new Kommune("3201", "Bærum", "Akershus"),
        new Kommune("3203", "Asker", "Akershus"),
        new Kommune("3205", "Lillestrøm", "Akershus"),
        new Kommune("3207", "Nordre Follo", "Akershus"),
        new Kommune("3209", "Ullensaker", "Akershus"),
        new Kommune("3212", "Nesodden", "Akershus"),
        new Kommune("3214", "Frogn", "Akershus"),
        new Kommune("3216", "Vestby", "Akershus"),
        new Kommune("3218", "Ås", "Akershus"),
        new Kommune("3220", "Enebakk", "Akershus"),
        new Kommune("3222", "Lørenskog", "Akershus"),
        new Kommune("3224", "Rælingen", "Akershus"),
        new Kommune("3226", "Aurskog-Høland", "Akershus"),
        new Kommune("3228", "Nes", "Akershus"),
        new Kommune("3230", "Gjerdrum", "Akershus"),
        new Kommune("3232", "Nittedal", "Akershus"),
        new Kommune("3234", "Lunner", "Akershus"),
        new Kommune("3236", "Jevnaker", "Akershus"),
        new Kommune("3238", "Nannestad", "Akershus"),
        new Kommune("3240", "Eidsvoll", "Akershus"),
        new Kommune("3242", "Hurdal", "Akershus"),
        new Kommune("3301", "Drammen", "Buskerud"),
        new Kommune("3303", "Kongsberg", "Buskerud"),
        new Kommune("3305", "Ringerike", "Buskerud"),
        new Kommune("3310", "Hole", "Buskerud"),
        new Kommune("3312", "Lier", "Buskerud"),
        new Kommune("3314", "Øvre Eiker", "Buskerud"),
        new Kommune("3316", "Modum", "Buskerud"),
        new Kommune("3318", "Krødsherad", "Buskerud"),
        new Kommune("3320", "Flå", "Buskerud"),
        new Kommune("3322", "Nesbyen", "Buskerud"),
        new Kommune("3324", "Gol", "Buskerud"),
        new Kommune("3326", "Hemsedal", "Buskerud"),
        new Kommune("3328", "Ål", "Buskerud"),
        new Kommune("3330", "Hol", "Buskerud"),
        new Kommune("3332", "Sigdal", "Buskerud"),
        new Kommune("3334", "Flesberg", "Buskerud"),
        new Kommune("3336", "Rollag", "Buskerud"),
        new Kommune("3338", "Nore og Uvdal", "Buskerud"),
        new Kommune("3401", "Kongsvinger", "Innlandet"),
        new Kommune("3403", "Hamar", "Innlandet"),
        new Kommune("3405", "Lillehammer", "Innlandet"),
        new Kommune("3407", "Gjøvik", "Innlandet"),
        new Kommune("3411", "Ringsaker", "Innlandet"),
        new Kommune("3412", "Løten", "Innlandet"),
        new Kommune("3413", "Stange", "Innlandet"),
        new Kommune("3414", "Nord-Odal", "Innlandet"),
        new Kommune("3415", "Sør-Odal", "Innlandet"),
        new Kommune("3416", "Eidskog", "Innlandet"),
        new Kommune("3417", "Grue", "Innlandet"),
        new Kommune("3418", "Åsnes", "Innlandet"),
        new Kommune("3419", "Våler (Innlandet)", "Innlandet"),
        new Kommune("3420", "Elverum", "Innlandet"),
        new Kommune("3421", "Trysil", "Innlandet"),
        new Kommune("3422", "Åmot", "Innlandet"),
        new Kommune("3423", "Stor-Elvdal", "Innlandet"),
        new Kommune("3424", "Rendalen", "Innlandet"),
        new Kommune("3425", "Engerdal", "Innlandet"),
        new Kommune("3426", "Tolga", "Innlandet"),
        new Kommune("3427", "Tynset", "Innlandet"),
        new Kommune("3428", "Alvdal", "Innlandet"),
        new Kommune("3429", "Folldal", "Innlandet"),
        new Kommune("3430", "Os", "Innlandet"),
        new Kommune("3431", "Dovre", "Innlandet"),
        new Kommune("3432", "Lesja", "Innlandet"),
        new Kommune("3433", "Skjåk", "Innlandet"),
        new Kommune("3434", "Lom", "Innlandet"),
        new Kommune("3435", "Vågå", "Innlandet"),
        new Kommune("3436", "Nord-Fron", "Innlandet"),
        new Kommune("3437", "Sel", "Innlandet"),
        new Kommune("3438", "Sør-Fron", "Innlandet"),
        new Kommune("3439", "Ringebu", "Innlandet"),
        new Kommune("3440", "Øyer", "Innlandet"),
        new Kommune("3441", "Gausdal", "Innlandet"),
        new Kommune("3442", "Østre Toten", "Innlandet"),
        new Kommune("3443", "Vestre Toten", "Innlandet"),
        new Kommune("3446", "Gran", "Innlandet"),
        new Kommune("3447", "Søndre Land", "Innlandet"),
        new Kommune("3448", "Nordre Land", "Innlandet"),
        new Kommune("3449", "Sør-Aurdal", "Innlandet"),
        new Kommune("3450", "Etnedal", "Innlandet"),
        new Kommune("3451", "Nord-Aurdal", "Innlandet"),
        new Kommune("3452", "Vestre Slidre", "Innlandet"),
        new Kommune("3453", "Øystre Slidre", "Innlandet"),
        new Kommune("3454", "Vang", "Innlandet"),
        new Kommune("3901", "Horten", "Vestfold"),
        new Kommune("3903", "Holmestrand", "Vestfold"),
        new Kommune("3905", "Tønsberg", "Vestfold"),
        new Kommune("3907", "Sandefjord", "Vestfold"),
        new Kommune("3909", "Larvik", "Vestfold"),
        new Kommune("3911", "Færder", "Vestfold"),
        new Kommune("4001", "Porsgrunn", "Telemark"),
        new Kommune("4003", "Skien", "Telemark"),
        new Kommune("4005", "Notodden", "Telemark"),
        new Kommune("4010", "Siljan", "Telemark"),
        new Kommune("4012", "Bamble", "Telemark"),
        new Kommune("4014", "Kragerø", "Telemark"),
        new Kommune("4016", "Drangedal", "Telemark"),
        new Kommune("4018", "Nome", "Telemark"),
        new Kommune("4020", "Midt-Telemark", "Telemark"),
        new Kommune("4022", "Seljord", "Telemark"),
        new Kommune("4024", "Hjartdal", "Telemark"),
        new Kommune("4026", "Tinn", "Telemark"),
        new Kommune("4028", "Kviteseid", "Telemark"),
        new Kommune("4030", "Nissedal", "Telemark"),
        new Kommune("4032", "Fyresdal", "Telemark"),
        new Kommune("4034", "Tokke", "Telemark"),
        new Kommune("4036", "Vinje", "Telemark"),
        new Kommune("4201", "Risør", "Agder"),
        new Kommune("4202", "Grimstad", "Agder"),
        new Kommune("4203", "Arendal", "Agder"),
        new Kommune("4204", "Kristiansand", "Agder"),
        new Kommune("4205", "Lindesnes", "Agder"),
        new Kommune("4206", "Farsund", "Agder"),
        new Kommune("4207", "Flekkefjord", "Agder"),
        new Kommune("4211", "Gjerstad", "Agder"),
        new Kommune("4212", "Vegårshei", "Agder"),
        new Kommune("4213", "Tvedestrand", "Agder"),
        new Kommune("4214", "Froland", "Agder"),
        new Kommune("4215", "Lillesand", "Agder"),
        new Kommune("4216", "Birkenes", "Agder"),
        new Kommune("4217", "Åmli", "Agder"),
        new Kommune("4218", "Iveland", "Agder"),
        new Kommune("4219", "Evje og Hornnes", "Agder"),
        new Kommune("4220", "Bygland", "Agder"),
        new Kommune("4221", "Valle", "Agder"),
        new Kommune("4222", "Bykle", "Agder"),
        new Kommune("4223", "Vennesla", "Agder"),
        new Kommune("4224", "Åseral", "Agder"),
        new Kommune("4225", "Lyngdal", "Agder"),
        new Kommune("4226", "Hægebostad", "Agder"),
        new Kommune("4227", "Kvinesdal", "Agder"),
        new Kommune("4228", "Sirdal", "Agder"),
        new Kommune("4601", "Bergen", "Vestland"),
        new Kommune("4602", "Kinn", "Vestland"),
        new Kommune("4611", "Etne", "Vestland"),
        new Kommune("4612", "Sveio", "Vestland"),
        new Kommune("4613", "Bømlo", "Vestland"),
        new Kommune("4614", "Stord", "Vestland"),
        new Kommune("4615", "Fitjar", "Vestland"),
        new Kommune("4616", "Tysnes", "Vestland"),
        new Kommune("4617", "Kvinnherad", "Vestland"),
        new Kommune("4618", "Ullensvang", "Vestland"),
        new Kommune("4619", "Eidfjord", "Vestland"),
        new Kommune("4620", "Ulvik", "Vestland"),
        new Kommune("4621", "Voss", "Vestland"),
        new Kommune("4622", "Kvam", "Vestland"),
        new Kommune("4623", "Samnanger", "Vestland"),
        new Kommune("4624", "Bjørnafjorden", "Vestland"),
        new Kommune("4625", "Austevoll", "Vestland"),
        new Kommune("4626", "Øygarden", "Vestland"),
        new Kommune("4627", "Askøy", "Vestland"),
        new Kommune("4628", "Vaksdal", "Vestland"),
        new Kommune("4629", "Modalen", "Vestland"),
        new Kommune("4630", "Osterøy", "Vestland"),
        new Kommune("4631", "Alver", "Vestland"),
        new Kommune("4632", "Austrheim", "Vestland"),
        new Kommune("4633", "Fedje", "Vestland"),
        new Kommune("4634", "Masfjorden", "Vestland"),
        new Kommune("4635", "Gulen", "Vestland"),
        new Kommune("4636", "Solund", "Vestland"),
        new Kommune("4637", "Hyllestad", "Vestland"),
        new Kommune("4638", "Høyanger", "Vestland"),
        new Kommune("4639", "Vik", "Vestland"),
        new Kommune("4640", "Sogndal", "Vestland"),
        new Kommune("4641", "Aurland", "Vestland"),
        new Kommune("4642", "Lærdal", "Vestland"),
        new Kommune("4643", "Årdal", "Vestland"),
        new Kommune("4644", "Luster", "Vestland"),
        new Kommune("4645", "Askvoll", "Vestland"),
        new Kommune("4646", "Fjaler", "Vestland"),
        new Kommune("4647", "Sunnfjord", "Vestland"),
        new Kommune("4648", "Bremanger", "Vestland"),
        new Kommune("4649", "Stad", "Vestland"),
        new Kommune("4650", "Gloppen", "Vestland"),
        new Kommune("4651", "Stryn", "Vestland"),
        new Kommune("5001", "Trondheim", "Trøndelag"),
        new Kommune("5006", "Steinkjer", "Trøndelag"),
        new Kommune("5007", "Namsos", "Trøndelag"),
        new Kommune("5014", "Frøya", "Trøndelag"),
        new Kommune("5020", "Osen", "Trøndelag"),
        new Kommune("5021", "Oppdal", "Trøndelag"),
        new Kommune("5022", "Rennebu", "Trøndelag"),
        new Kommune("5025", "Røros", "Trøndelag"),
        new Kommune("5026", "Holtålen", "Trøndelag"),
        new Kommune("5027", "Midtre Gauldal", "Trøndelag"),
        new Kommune("5028", "Melhus", "Trøndelag"),
        new Kommune("5029", "Skaun", "Trøndelag"),
        new Kommune("5031", "Malvik", "Trøndelag"),
        new Kommune("5032", "Selbu", "Trøndelag"),
        new Kommune("5033", "Tydal", "Trøndelag"),
        new Kommune("5034", "Meråker", "Trøndelag"),
        new Kommune("5035", "Stjørdal", "Trøndelag"),
        new Kommune("5036", "Frosta", "Trøndelag"),
        new Kommune("5037", "Levanger", "Trøndelag"),
        new Kommune("5038", "Verdal", "Trøndelag"),
        new Kommune("5041", "Snåsa", "Trøndelag"),
        new Kommune("5042", "Lierne", "Trøndelag"),
        new Kommune("5043", "Røyrvik", "Trøndelag"),
        new Kommune("5044", "Namsskogan", "Trøndelag"),
        new Kommune("5045", "Grong", "Trøndelag"),
        new Kommune("5046", "Høylandet", "Trøndelag"),
        new Kommune("5047", "Overhalla", "Trøndelag"),
        new Kommune("5049", "Flatanger", "Trøndelag"),
        new Kommune("5052", "Leka", "Trøndelag"),
        new Kommune("5053", "Inderøy", "Trøndelag"),
        new Kommune("5054", "Indre Fosen", "Trøndelag"),
        new Kommune("5055", "Heim", "Trøndelag"),
        new Kommune("5056", "Hitra", "Trøndelag"),
        new Kommune("5057", "Ørland", "Trøndelag"),
        new Kommune("5058", "Åfjord", "Trøndelag"),
        new Kommune("5059", "Orkland", "Trøndelag"),
        new Kommune("5060", "Nærøysund", "Trøndelag"),
        new Kommune("5061", "Rindal", "Trøndelag"),
        new Kommune("5501", "Tromsø", "Troms"),
        new Kommune("5503", "Harstad", "Troms"),
        new Kommune("5510", "Kvæfjord", "Troms"),
        new Kommune("5512", "Tjeldsund", "Troms"),
        new Kommune("5514", "Ibestad", "Troms"),
        new Kommune("5516", "Gratangen", "Troms"),
        new Kommune("5518", "Lavangen", "Troms"),
        new Kommune("5520", "Bardu", "Troms"),
        new Kommune("5522", "Salangen", "Troms"),
        new Kommune("5524", "Målselv", "Troms"),
        new Kommune("5526", "Sørreisa", "Troms"),
        new Kommune("5528", "Dyrøy", "Troms"),
        new Kommune("5530", "Senja", "Troms"),
        new Kommune("5532", "Balsfjord", "Troms"),
        new Kommune("5534", "Karlsøy", "Troms"),
        new Kommune("5536", "Lyngen", "Troms"),
        new Kommune("5538", "Storfjord", "Troms"),
        new Kommune("5540", "Kåfjord", "Troms"),
        new Kommune("5542", "Skjervøy", "Troms"),
        new Kommune("5544", "Nordreisa", "Troms"),
        new Kommune("5546", "Kvænangen", "Troms"),
        new Kommune("5601", "Alta", "Finnmark"),
        new Kommune("5603", "Hammerfest", "Finnmark"),
        new Kommune("5605", "Sør-Varanger", "Finnmark"),
        new Kommune("5607", "Vadsø", "Finnmark"),
        new Kommune("5610", "Karasjok", "Finnmark"),
        new Kommune("5612", "Kautokeino", "Finnmark"),
        new Kommune("5614", "Loppa", "Finnmark"),
        new Kommune("5616", "Hasvik", "Finnmark"),
        new Kommune("5618", "Måsøy", "Finnmark"),
        new Kommune("5620", "Nordkapp", "Finnmark"),
        new Kommune("5622", "Porsanger", "Finnmark"),
        new Kommune("5624", "Lebesby", "Finnmark"),
        new Kommune("5626", "Gamvik", "Finnmark"),
        new Kommune("5628", "Tana", "Finnmark"),
        new Kommune("5630", "Berlevåg", "Finnmark"),
        new Kommune("5632", "Båtsfjord", "Finnmark"),
        new Kommune("5634", "Vardø", "Finnmark"),
        new Kommune("5636", "Nesseby", "Finnmark"),
    };

    // Fylkene i samme rekkefølge som kommunenumrene (Oslo, Rogaland, ... Finnmark).
    // Lages én gang fra lista over, så hvert fylke bare står der én gang.
    public static readonly List<string> Fylker = LagFylkeliste();

    private static List<string> LagFylkeliste()
    {
        var fylker = new List<string>();
        foreach (Kommune kommune in Alle)
        {
            if (!fylker.Contains(kommune.Fylke))
            {
                fylker.Add(kommune.Fylke);
            }
        }
        return fylker;
    }

    // Kommunene i ett fylke, til nedtrekkslistene.
    public static List<Kommune> IFylke(string fylke)
    {
        var kommuner = new List<Kommune>();
        foreach (Kommune kommune in Alle)
        {
            if (kommune.Fylke == fylke)
            {
                kommuner.Add(kommune);
            }
        }
        return kommuner;
    }

    // Gir kommunen med dette nummeret, eller null hvis nummeret ikke finnes.
    public static Kommune? Finn(string? nummer)
    {
        if (string.IsNullOrWhiteSpace(nummer))
        {
            return null;
        }

        foreach (Kommune kommune in Alle)
        {
            if (kommune.Nummer == nummer.Trim())
            {
                return kommune;
            }
        }
        return null;
    }

    public static bool Finnes(string? nummer)
    {
        return Finn(nummer) != null;
    }

    // Finner kommunen ut fra det brukeren skrev i kommunefeltet. Godtar navnet
    // («Grimstad», store eller små bokstaver, også «Grimstad kommune») eller nummeret («4202»).
    // Gir kommunen, eller null hvis den ikke finnes.
    public static Kommune? FinnFraTekst(string? tekst)
    {
        if (string.IsNullOrWhiteSpace(tekst))
        {
            return null;
        }

        string renset = tekst.Trim();
        if (renset.EndsWith(" kommune", StringComparison.OrdinalIgnoreCase))
        {
            renset = renset.Substring(0, renset.Length - " kommune".Length).Trim();
        }

        foreach (Kommune kommune in Alle)
        {
            if (kommune.Nummer == renset || string.Equals(kommune.Navn, renset, StringComparison.OrdinalIgnoreCase))
            {
                return kommune;
            }
        }
        return null;
    }

    // Navnet til kommunen med dette nummeret, eller tom tekst. Brukes til å fylle kommunefeltet.
    public static string NavnEllerTomt(string? nummer)
    {
        Kommune? kommune = Finn(nummer);
        if (kommune == null)
        {
            return "";
        }
        return kommune.Navn;
    }

    // Navnet som vises i lister og utskrift.
    public static string VisNavn(string? nummer)
    {
        Kommune? kommune = Finn(nummer);
        if (kommune == null)
        {
            return Nabohjelp.Services.Oversetter.Tekst(IkkeOppgitt);
        }
        return kommune.Navn;
    }
}
