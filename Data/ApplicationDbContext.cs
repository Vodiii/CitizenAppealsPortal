using CitizenAppealsPortal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CitizenAppealsPortal.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Appeal> Appeals { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<District> Districts { get; set; }
    public DbSet<AppealResponse> AppealResponses { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<DeputyTerm> DeputyTerms { get; set; }   // новая таблица

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Appeals
        builder.Entity<Appeal>()
            .HasOne(a => a.Citizen)
            .WithMany(u => u.Appeals)
            .HasForeignKey(a => a.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appeal>()
            .HasOne(a => a.District)
            .WithMany(d => d.Appeals)
            .HasForeignKey(a => a.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appeal>()
            .HasOne(a => a.Category)
            .WithMany(c => c.Appeals)
            .HasForeignKey(a => a.CategoryId);

        // AppealResponses
        builder.Entity<AppealResponse>()
            .HasOne(r => r.Appeal)
            .WithMany(a => a.Responses)
            .HasForeignKey(r => r.AppealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AppealResponse>()
            .HasOne(r => r.Author)
            .WithMany()
            .HasForeignKey(r => r.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Photos
        builder.Entity<Photo>()
            .HasOne(p => p.Appeal)
            .WithMany(a => a.Photos)
            .HasForeignKey(p => p.AppealId)
            .OnDelete(DeleteBehavior.Cascade);

        // District -> Deputy (one-to-many через AssignedDistrictId)
        builder.Entity<District>()
            .HasMany(d => d.Deputies)
            .WithOne(u => u.AssignedDistrict)
            .HasForeignKey(u => u.AssignedDistrictId)
            .OnDelete(DeleteBehavior.SetNull);

        // DeputyTerms
        builder.Entity<DeputyTerm>()
            .HasOne(dt => dt.Deputy)
            .WithMany(u => u.DeputyTerms)      // нужно добавить коллекцию в ApplicationUser
            .HasForeignKey(dt => dt.DeputyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Индексы
        builder.Entity<District>()
            .HasIndex(d => d.Name)
            .IsUnique();

        builder.Entity<District>()
            .HasIndex(d => d.Boundary)
            .HasMethod("GIST");
    }
}