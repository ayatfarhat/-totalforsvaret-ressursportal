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

    public BehovController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "Admin,Kommune,Frivillig")]
    public IActionResult Opprett()
    {
        return View(new BehovViewModel());
    }
    [HttpPost, ValidateAntiForgeryToken]           
    [Authorize(Roles = "Admin,Kommune,Frivillig")]
    public async Task<IActionResult> Opprett(BehovViewModel vm)
    {
        
        if (!ModelState.IsValid)
            return View(vm);                      

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
            OpprettetAvBrukerId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        _db.Behovsliste.Add(behov);
        await _db.SaveChangesAsync();            

        return RedirectToAction(nameof(Ko));
    }

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
            MuligeNesteStatuser = TillatteManuelleOvergangar(b.Status)
        }).ToList();

        return View(visning);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Kommune")]
    public async Task<IActionResult> EndreStatus(int id, BehovStatus nyStatus)
    {
        var behov = await _db.Behovsliste.FindAsync(id);
        if (behov is null) return NotFound();

        if (!TillatteManuelleOvergangar(behov.Status).Contains(nyStatus))
        {
            TempData["Feil"] = $"Ugyldig statusendring: {behov.Status} → {nyStatus}.";
            return RedirectToAction(nameof(Ko));
        }

        behov.Status = nyStatus;
        behov.SistEndretTid = DateTime.UtcNow;                                   
        behov.SistEndretAvBrukerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Ko));
    }

    private static List<BehovStatus> TillatteManuelleOvergangar(BehovStatus fra) => fra switch
    {
        BehovStatus.New         => new() { BehovStatus.UnderReview },
        BehovStatus.UnderReview => new() { BehovStatus.Resolved },   
        BehovStatus.Assigned    => new() { BehovStatus.Resolved },
        _                       => new()
    };
}
