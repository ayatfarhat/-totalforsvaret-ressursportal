using System.Security.Claims;
using Nabohjelp.Data;
using Nabohjelp.Models;
using Nabohjelp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nabohjelp.Controllers;

[Authorize]
public class BehovController : Controller
{
    private readonly ApplicationDbContext _db;

    public BehovController(ApplicationDbContext db)
    {
        _db = db;
    }

    // GET: /Behov/Opprett
    [HttpGet]
    [Authorize(Roles = "Admin,Kommune,Frivillig")]
    public IActionResult Opprett()
    {
        return View(new BehovViewModel());
    }

    // POST: /Behov/Opprett
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Kommune,Frivillig")]
    public async Task<IActionResult> Opprett(BehovViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var behov = new Behov
        {
            Type = vm.Type!.Value,
            Beskrivelse = vm.Beskrivelse,
            Latitude = vm.Latitude!.Value,
            Longitude = vm.Longitude!.Value,
            Prioritet = vm.Prioritet!.Value,
            KontaktTelefon = vm.KontaktTelefon,
            KontaktEpost = vm.KontaktEpost,
            Status = BehovStatus.New,
            OpprettetTid = DateTime.UtcNow,
            OpprettetAvBrukerId =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        _db.Behovsliste.Add(behov);
        await _db.SaveChangesAsync();

        // Admin og Kommune sendes til behovskøen.
        if (User.IsInRole("Admin") || User.IsInRole("Kommune"))
        {
            return RedirectToAction(nameof(Ko));
        }

        // Frivillige får bekreftelse og en knapp til Mine behov.
TempData["Melding"] = "Ditt behov er registrert.";
TempData["VisMineBehovKnapp"] = true;

return RedirectToAction("Index", "Home");
    }

    // GET: /Behov/MineBehov
    // Frivillige ser bare egne behov.
    // Admin og Kommune ser alle behov.
    [HttpGet]
    [Authorize(Roles = "Admin,Kommune,Frivillig")]
    public async Task<IActionResult> MineBehov()
    {
        var brukerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(brukerId))
        {
            return Challenge();
        }

        var sporring = _db.Behovsliste.AsNoTracking();

        // Begrens til egne behov for Frivillig.
        if (!User.IsInRole("Admin") &&
            !User.IsInRole("Kommune"))
        {
            sporring = sporring.Where(
                b => b.OpprettetAvBrukerId == brukerId);
        }

        var behov = await sporring
            .OrderByDescending(b => b.OpprettetTid)
            .ToListAsync();

        return View(behov);
    }

    // GET: /Behov/Ko
    // Kun Admin og Kommune har tilgang.
    [HttpGet]
    [Authorize(Roles = "Admin,Kommune")]
    public async Task<IActionResult> Ko()
    {
        var liste = await _db.Behovsliste
            .Where(b => b.Status != BehovStatus.Resolved)
            .OrderBy(b => b.Prioritet)
            .ThenBy(b => b.OpprettetTid)
            .ToListAsync();

        var visning = liste.Select(b => new BehovKoItem
        {
            Id = b.Id,
            Type = b.Type,
            Prioritet = b.Prioritet,
            Status = b.Status,
            Beskrivelse = b.Beskrivelse,
            OpprettetTid = b.OpprettetTid,
            MuligeNesteStatuser =
                TillatteManuelleOvergangar(b.Status)
        }).ToList();

        return View(visning);
    }

    // POST: /Behov/EndreStatus
    // Kun Admin og Kommune kan endre status.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Kommune")]
    public async Task<IActionResult> EndreStatus(
        int id,
        BehovStatus nyStatus)
    {
        var behov = await _db.Behovsliste.FindAsync(id);

        if (behov is null)
        {
            return NotFound();
        }

        if (!TillatteManuelleOvergangar(behov.Status)
            .Contains(nyStatus))
        {
            TempData["Feil"] =
                $"Kan ikke endre status fra «{Visningstekst.Status(behov.Status)}» " +
                $"til «{Visningstekst.Status(nyStatus)}».";

            return RedirectToAction(nameof(Ko));
        }

        behov.Status = nyStatus;
        behov.SistEndretTid = DateTime.UtcNow;
        behov.SistEndretAvBrukerId =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Ko));
    }

    // Tillatte statusendringer.
    private static List<BehovStatus> TillatteManuelleOvergangar(
        BehovStatus fra) => fra switch
    {
        BehovStatus.New =>
            new() { BehovStatus.UnderReview },

        BehovStatus.UnderReview =>
            new() { BehovStatus.Resolved },

        BehovStatus.Assigned =>
            new() { BehovStatus.Resolved },

        _ => new()
    };
}