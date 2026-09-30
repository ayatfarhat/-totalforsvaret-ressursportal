namespace Kommandobro.Models;
public enum BehovType { Transport, Drone, Generator, Evakuering, Annet }
public enum BehovPrioritet { Urgent = 0, Planned = 1 }   
public enum BehovStatus { New, UnderReview, Assigned, Resolved }
public class Behov
{
    public int Id { get; set; }                       
    public BehovType Type { get; set; }
    public string? Beskrivelse { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public BehovPrioritet Prioritet { get; set; }
    public BehovStatus Status { get; set; } = BehovStatus.New;   
    public string? KontaktTelefon { get; set; }
    public string? KontaktEpost { get; set; }
    public DateTime OpprettetTid { get; set; } = DateTime.UtcNow;
    public string? OpprettetAvBrukerId { get; set; }
    public DateTime? SistEndretTid { get; set; }
    public string? SistEndretAvBrukerId { get; set; }
}
