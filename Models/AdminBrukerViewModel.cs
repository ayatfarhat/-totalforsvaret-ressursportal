namespace Nabohjelp.Models;

// Én rad i brukerlisten på admin-siden.
public class AdminBrukerViewModel
{
    public string Id { get; set; } = "";
    public string Epost { get; set; } = "";
    public string Rolle { get; set; } = "";
    public bool TofaErPa { get; set; }
    public bool ErLaast { get; set; }

    // Admin kan ikke endre, låse eller slette sin egen konto fra listen.
    public bool ErMeg { get; set; }
}
