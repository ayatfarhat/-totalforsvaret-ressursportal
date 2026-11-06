namespace Nabohjelp.ViewModels;

// Det som vises på feilsiden (Views/Shared/Error.cshtml).
public class ErrorViewModel
{
    // En kode som gjør det lettere å finne feilen i loggen.
    public string? RequestId { get; set; }

    public bool ShowRequestId
    {
        get { return !string.IsNullOrEmpty(RequestId); }
    }
}
