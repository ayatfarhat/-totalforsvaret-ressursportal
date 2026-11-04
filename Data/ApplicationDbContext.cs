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
    public DbSet<Ressurs> Ressurser { get; set; } = null!;
    public DbSet<Tildeling> Tildelinger { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
{
    // Beholder Identity-oppsettet for brukere og roller.
    base.OnModelCreating(builder);

    // Kobler hver tildeling til ett behov.
    builder.Entity<Tildeling>()
        .HasOne(t => t.Behov)
        .WithMany()
        .HasForeignKey(t => t.BehovId)
        .OnDelete(DeleteBehavior.Restrict);

    // Kobler hver tildeling til én ressurs.
    builder.Entity<Tildeling>()
        .HasOne(t => t.Ressurs)
        .WithMany()
        .HasForeignKey(t => t.RessursId)
        .OnDelete(DeleteBehavior.Restrict);

    // Registrerer hvilken bruker som utførte tildelingen.
    builder.Entity<Tildeling>()
        .HasOne(t => t.TildeltAvBruker)
        .WithMany()
        .HasForeignKey(t => t.TildeltAvBrukerId)
        .OnDelete(DeleteBehavior.Restrict);

    // Restrict hindrer sletting av behov, ressurs eller bruker
    // så lenge en tildeling viser til dem.
}
}