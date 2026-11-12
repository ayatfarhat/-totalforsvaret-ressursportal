namespace Nabohjelp.ViewModels;

// Det forsiden trenger for innloggede brukere: snarveien «Mine registreringer»
// med antall behov og ressurser, og varsler om tildelte ressurser.
// Testpersonen i brukervennlighetstesten brukte tid på å finne registreringene sine.
public class ForsideViewModel
{
    public bool ErInnlogget { get; set; }

    public int AntallBehov { get; set; }

    public int AntallRessurser { get; set; }

    public List<TildelingsvarselViewModel> Varsler { get; set; } = new List<TildelingsvarselViewModel>();
}
