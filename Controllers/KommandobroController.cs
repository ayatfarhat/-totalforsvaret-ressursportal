using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kriseportal.Controllers
{
    // Bare Admin og Kommune (Hassans roller) kommer inn her. Alle andre blir
    // sendt til /Account/AccessDenied. Ikke logget inn i det hele tatt ->
    // sendt videre til innloggingssiden automatisk.
    // NB: Rollenavnene her ("Admin", "Kommune") er de Hassan faktisk har laget.
    // Oppgaveteksten nevner "kriseoperatør" som eget rollenavn - avklar med
    // Hassan/gruppa om det skal være en egen rolle, eller om "Kommune" er ment
    // å dekke det samme.
    [Authorize(Roles = "Admin,Kommune")]
    public class KommandobroController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
