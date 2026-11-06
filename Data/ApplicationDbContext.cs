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

    // Ressurser som kommunen eller kriseledelsen ønsker at innbyggere registrerer.
    public DbSet<OnsketRessurs> OnskedeRessurser { get; set; } = null!;

    // Invitasjonslenker som gir en rolle (se Services/Invitasjonsregler.cs).
    public DbSet<Invitasjon> Invitasjoner { get; set; } = null!;

    // Tildeling kobler et behov, en ressurs og brukeren som tildelte.
    // Restrict betyr at et behov, en ressurs eller en bruker ikke kan slettes
    // så lenge en tildeling viser til dem. Da beholdes historikken.
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Beholder Identity-oppsettet for brukere og roller.
        base.OnModelCreating(builder);

        // t => t.Behov betyr «for en tildeling t: egenskapen Behov». EF bruker det
        // til å vite hvilke felt koblingen gjelder.
        builder.Entity<Tildeling>()
            .HasOne(t => t.Behov)
            .WithMany()
            .HasForeignKey(t => t.BehovId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Tildeling>()
            .HasOne(t => t.Ressurs)
            .WithMany()
            .HasForeignKey(t => t.RessursId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Tildeling>()
            .HasOne(t => t.TildeltAvBruker)
            .WithMany()
            .HasForeignKey(t => t.TildeltAvBrukerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hver kode finnes bare én gang, og databasen kan slå den opp raskt.
        builder.Entity<Invitasjon>()
            .HasIndex(i => i.KodeHash)
            .IsUnique();
    }
}
