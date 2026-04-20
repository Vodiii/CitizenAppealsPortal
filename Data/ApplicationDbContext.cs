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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Связь ApplicationUser -> Appeals
        builder.Entity<Appeal>()
            .HasOne(a => a.Citizen)
            .WithMany(u => u.Appeals)
            .HasForeignKey(a => a.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        // Связь Appeal -> District
        builder.Entity<Appeal>()
            .HasOne(a => a.District)
            .WithMany(d => d.Appeals)
            .HasForeignKey(a => a.DistrictId)
            .OnDelete(DeleteBehavior.Restrict);

        // Связь Appeal -> Category
        builder.Entity<Appeal>()
            .HasOne(a => a.Category)
            .WithMany(c => c.Appeals)
            .HasForeignKey(a => a.CategoryId);

        // Связь AppealResponse -> Appeal
        builder.Entity<AppealResponse>()
            .HasOne(r => r.Appeal)
            .WithMany(a => a.Responses)
            .HasForeignKey(r => r.AppealId)
            .OnDelete(DeleteBehavior.Cascade);

        // Связь AppealResponse -> Author
        builder.Entity<AppealResponse>()
            .HasOne(r => r.Author)
            .WithMany()
            .HasForeignKey(r => r.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Связь Photo -> Appeal
        builder.Entity<Photo>()
            .HasOne(p => p.Appeal)
            .WithMany(a => a.Photos)
            .HasForeignKey(p => p.AppealId)
            .OnDelete(DeleteBehavior.Cascade);

        // Связь District -> Deputies (один-ко-многим)
        builder.Entity<District>()
            .HasMany(d => d.Deputies)
            .WithOne(u => u.AssignedDistrict)
            .HasForeignKey(u => u.AssignedDistrictId)
            .OnDelete(DeleteBehavior.SetNull);

        // Уникальный индекс для названия округа
        builder.Entity<District>()
            .HasIndex(d => d.Name)
            .IsUnique();

        // Пространственный индекс для границ округа (GIST)
        builder.Entity<District>()
            .HasIndex(d => d.Boundary)
            .HasMethod("GIST");
    }
}