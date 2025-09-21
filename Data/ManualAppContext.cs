using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ManualApp.Models;
using ManualApp.Services;

namespace ManualApp.Data
{
    public class ManualAppContext : IdentityDbContext<ApplicationUser>
    {
        private readonly ICurrentUserService _current;
        public ManualAppContext(DbContextOptions<ManualAppContext> options, ICurrentUserService current)
            : base(options)
        {
            _current = current;
        }

        public DbSet<Manual> Manuals => Set<Manual>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Content> Contents => Set<Content>();
        public DbSet<Image> Images => Set<Image>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Manual>().HasQueryFilter(m =>
                _current.IsAuthenticated &&
                (_current.IsAdmin || m.OwnerId == _current.UserId));

            // ManualとCategoryの関係
            modelBuilder.Entity<Manual>()
                .HasOne(m => m.Category)
                .WithMany(c => c.Manuals)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ManualとContentの関係
            modelBuilder.Entity<Content>()
                .HasOne(c => c.Manual)
                .WithMany(m => m.Contents)
                .HasForeignKey(c => c.ManualId)
                .OnDelete(DeleteBehavior.Cascade);

            // ContentとImageの1対1関係
            modelBuilder.Entity<Content>()
                .HasOne(c => c.Image)
                .WithOne(i => i.Content)
                .HasForeignKey<Image>(i => i.ContentId)
                .OnDelete(DeleteBehavior.Cascade);

            // CategoryとApplicationUserの関係
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Owner)
                .WithMany()
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}