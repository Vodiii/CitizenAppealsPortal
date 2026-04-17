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

        builder.Entity<Photo>()
            .HasOne(p => p.Appeal)
            .WithMany(a => a.Photos)
            .HasForeignKey(p => p.AppealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<District>()
            .HasIndex(d => d.Name)
            .IsUnique();

        builder.Entity<District>()
            .HasIndex(d => d.Boundary)
            .HasMethod("GIST");
    }
}