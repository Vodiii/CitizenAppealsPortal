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
    public DbSet<DeputyTerm> DeputyTerms { get; set; }
    public DbSet<AppealVote> AppealVotes { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Comment> Comments { get; set; }

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

        // District -> Deputy (один-ко-многим)
        builder.Entity<District>()
            .HasMany(d => d.Deputies)
            .WithOne(u => u.AssignedDistrict)
            .HasForeignKey(u => u.AssignedDistrictId)
            .OnDelete(DeleteBehavior.SetNull);

        // DeputyTerms
        builder.Entity<DeputyTerm>()
            .HasOne(dt => dt.Deputy)
            .WithMany(u => u.DeputyTerms)
            .HasForeignKey(dt => dt.DeputyId)
            .OnDelete(DeleteBehavior.Cascade);

        // AppealVote
        builder.Entity<AppealVote>()
            .HasOne(v => v.Appeal)
            .WithMany(a => a.Votes)
            .HasForeignKey(v => v.AppealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AppealVote>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AppealVote>()
            .HasIndex(v => new { v.AppealId, v.UserId })
            .IsUnique();

        // Notification
        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Notification>()
            .HasOne(n => n.Appeal)
            .WithMany()
            .HasForeignKey(n => n.AppealId)
            .OnDelete(DeleteBehavior.Cascade);

        // Comment
        builder.Entity<Comment>()
            .HasOne(c => c.Appeal)
            .WithMany(a => a.Comments)
            .HasForeignKey(c => c.AppealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Comment>()
            .HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<District>()
            .HasIndex(d => d.Name)
            .IsUnique();

        builder.Entity<District>()
            .HasIndex(d => d.Boundary)
            .HasMethod("GIST");
    }
}