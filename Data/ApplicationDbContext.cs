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
    public DbSet<UserDocument> UserDocuments { get; set; }
    public DbSet<UserSetting> UserSettings { get; set; }
    public DbSet<UserLoginHistory> UserLoginHistories { get; set; }
    public DbSet<UserCategorySubscription> UserCategorySubscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // === Appeals ===
        builder.Entity<Appeal>(appeal =>
        {
            appeal.HasOne(a => a.Citizen)
                  .WithMany(u => u.Appeals)
                  .HasForeignKey(a => a.CitizenId)
                  .OnDelete(DeleteBehavior.Restrict);

            appeal.HasOne(a => a.District)
                  .WithMany(d => d.Appeals)
                  .HasForeignKey(a => a.DistrictId)
                  .OnDelete(DeleteBehavior.Restrict);

            appeal.HasOne(a => a.Category)
                  .WithMany(c => c.Appeals)
                  .HasForeignKey(a => a.CategoryId);

            appeal.HasIndex(a => a.CitizenId);
            appeal.HasIndex(a => a.DistrictId);
            appeal.HasIndex(a => a.Status);
            appeal.HasIndex(a => a.CreatedAt);
        });

        // === AppealResponses ===
        builder.Entity<AppealResponse>(response =>
        {
            response.HasOne(r => r.Appeal)
                    .WithMany(a => a.Responses)
                    .HasForeignKey(r => r.AppealId)
                    .OnDelete(DeleteBehavior.Cascade);

            response.HasOne(r => r.Author)
                    .WithMany()
                    .HasForeignKey(r => r.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

            response.HasIndex(r => r.AppealId);
        });

        // === Photos ===
        builder.Entity<Photo>(photo =>
        {
            photo.HasOne(p => p.Appeal)
                 .WithMany(a => a.Photos)
                 .HasForeignKey(p => p.AppealId)
                 .OnDelete(DeleteBehavior.Cascade);

            photo.HasIndex(p => p.AppealId);
        });

        // === Districts ===
        builder.Entity<District>(district =>
        {
            district.HasMany(d => d.Deputies)
                    .WithOne(u => u.AssignedDistrict)
                    .HasForeignKey(u => u.AssignedDistrictId)
                    .OnDelete(DeleteBehavior.SetNull);

            district.HasIndex(d => d.Name).IsUnique();
            district.HasIndex(d => d.Boundary).HasMethod("GIST");
        });

        // === DeputyTerms ===
        builder.Entity<DeputyTerm>(term =>
        {
            term.HasOne(dt => dt.Deputy)
                .WithMany(u => u.DeputyTerms)
                .HasForeignKey(dt => dt.DeputyId)
                .OnDelete(DeleteBehavior.Cascade);

            term.HasIndex(dt => dt.DeputyId);
        });

        // === AppealVotes ===
        builder.Entity<AppealVote>(vote =>
        {
            vote.HasOne(v => v.Appeal)
                .WithMany(a => a.Votes)
                .HasForeignKey(v => v.AppealId)
                .OnDelete(DeleteBehavior.Cascade);

            vote.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            vote.HasIndex(v => new { v.AppealId, v.UserId }).IsUnique();
        });

        // === Notifications ===
        builder.Entity<Notification>(notification =>
        {
            notification.HasOne(n => n.User)
                        .WithMany()
                        .HasForeignKey(n => n.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

            notification.HasOne(n => n.Appeal)
                        .WithMany()
                        .HasForeignKey(n => n.AppealId)
                        .OnDelete(DeleteBehavior.Cascade);

            notification.HasIndex(n => n.UserId);
        });

        // === Comments ===
        builder.Entity<Comment>(comment =>
        {
            comment.HasOne(c => c.Appeal)
                   .WithMany(a => a.Comments)
                   .HasForeignKey(c => c.AppealId)
                   .OnDelete(DeleteBehavior.Cascade);

            comment.HasOne(c => c.Author)
                   .WithMany()
                   .HasForeignKey(c => c.AuthorId)
                   .OnDelete(DeleteBehavior.Restrict);

            comment.HasIndex(c => c.AppealId);
        });

        // === Профиль пользователя ===
        builder.Entity<UserDocument>(doc =>
        {
            doc.HasOne(d => d.User)
               .WithMany(u => u.Documents)
               .HasForeignKey(d => d.UserId)
               .OnDelete(DeleteBehavior.Cascade);

            doc.HasIndex(d => d.UserId);
        });

        builder.Entity<UserSetting>(setting =>
        {
            setting.HasOne(s => s.User)
                   .WithMany(u => u.Settings)
                   .HasForeignKey(s => s.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            setting.HasIndex(s => s.UserId);
        });

        builder.Entity<UserLoginHistory>(history =>
        {
            history.HasOne(l => l.User)
                   .WithMany(u => u.LoginHistory)
                   .HasForeignKey(l => l.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            history.HasIndex(l => l.UserId);
        });

        builder.Entity<UserCategorySubscription>(subscription =>
        {
            subscription.HasOne(s => s.User)
                        .WithMany(u => u.CategorySubscriptions)
                        .HasForeignKey(s => s.UserId)
                        .OnDelete(DeleteBehavior.Cascade);

            subscription.HasOne(s => s.Category)
                        .WithMany()
                        .HasForeignKey(s => s.CategoryId)
                        .OnDelete(DeleteBehavior.Cascade);

            subscription.HasIndex(s => s.UserId);
        });
    }
}