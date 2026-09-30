using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nabohjelp.Models;

namespace Nabohjelp.Data;

// Databasen for brukere, roller og applikasjonens data.
public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Behov som er registrert i systemet.
    // Gjør at BehovController kan hente og lagre behov.
    public DbSet<Behov> Behovsliste { get; set; } = null!;
}