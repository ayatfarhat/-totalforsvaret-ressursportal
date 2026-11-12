using System.ComponentModel.DataAnnotations;

namespace Nabohjelp.Models;

// Én linje i historikken til et behov: hvilken status det fikk, når, og hvem som gjorde det.
// Lagres hver gang statusen endres (BehovController og TildelingController), og vises
// under «Historikk» på «Mine behov» og i behovskøen.
//
// At behovet ble meldt, lagres ikke her. Det står allerede i Behov.OpprettetTid.
// Slettes behovet, slettes historikken også (se Data/ApplicationDbContext.cs).
public class StatusEndring
{
    public int Id { get; set; }

    public int BehovId { get; set; }

    public BehovStatus FraStatus { get; set; }
    public BehovStatus TilStatus { get; set; }

    public DateTime Tid { get; set; } = DateTime.UtcNow;

    // Hvem som gjorde endringen. Blir tom hvis kontoen senere slettes (se KontoService).
    public string? EndretAvBrukerId { get; set; }

    // Ressursen det gjelder, når endringen er en tildeling eller en angret tildeling.
    // Vi lagrer typen som tekst (for eksempel «Aggregat»), så historikken kan vises
    // selv om ressursen senere blir slettet.
    [MaxLength(100)]
    public string? RessursType { get; set; }

    [MaxLength(100)]
    public string? RessursAnnetType { get; set; }

    // true når en tildeling ble angret (se TildelingController.Angre).
    public bool ErAngret { get; set; }

    // Lager en vanlig statusendring, for eksempel «Ny» til «Under vurdering».
    public static StatusEndring Ny(int behovId, BehovStatus fra, BehovStatus til, string? brukerId)
    {
        return new StatusEndring
        {
            BehovId = behovId,
            FraStatus = fra,
            TilStatus = til,
            EndretAvBrukerId = brukerId,
            Tid = DateTime.UtcNow
        };
    }

    // Lager en linje for en tildeling, eller for en tildeling som ble angret.
    public static StatusEndring ForTildeling(int behovId, BehovStatus fra, BehovStatus til, string? brukerId, Ressurs ressurs, bool erAngret)
    {
        StatusEndring endring = Ny(behovId, fra, til, brukerId);
        endring.RessursType = ressurs.Type;
        endring.RessursAnnetType = ressurs.AnnetType;
        endring.ErAngret = erAngret;
        return endring;
    }
}
