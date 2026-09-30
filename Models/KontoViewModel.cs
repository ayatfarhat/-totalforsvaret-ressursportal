namespace Kriseportal.Models;

// Det som vises på "Min konto"
public class KontoViewModel
{
    public string Epost { get; set; } = "";
    public string Rolle { get; set; } = "";
    public bool TofaErPa { get; set; }
}
