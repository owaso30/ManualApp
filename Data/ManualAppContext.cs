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
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();
        public DbSet<GroupJoinRequest> GroupJoinRequests => Set<GroupJoinRequest>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // グローバルクエリフィルターを無効化（グループモード対応のため）
            // modelBuilder.Entity<Manual>().HasQueryFilter(m =>
            //     _current.IsAuthenticated &&
            //     (_current.IsAdmin || m.OwnerId == _current.UserId));

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

            // CategoryとApplicationUserの関係（外部キー制約なし - グループ所有対応）
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Owner)
                .WithMany()
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false); // 外部キー制約を無効化

            // ManualCountは計算プロパティなのでデータベースに保存しない
            modelBuilder.Entity<Category>()
                .Ignore(c => c.ManualCount);

            // ManualとApplicationUserの関係（外部キー制約なし - グループ所有対応）
            modelBuilder.Entity<Manual>()
                .HasOne(m => m.Owner)
                .WithMany()
                .HasForeignKey(m => m.OwnerId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false); // 外部キー制約を無効化

            // GroupとApplicationUserの関係（グループ作成者）
            modelBuilder.Entity<Group>()
                .HasOne(g => g.Owner)
                .WithMany(u => u.OwnedGroups)
                .HasForeignKey(g => g.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // GroupMembershipの関係
            modelBuilder.Entity<GroupMembership>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Memberships)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupMembership>()
                .HasOne(gm => gm.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // GroupJoinRequestの関係
            modelBuilder.Entity<GroupJoinRequest>()
                .HasOne(gjr => gjr.Group)
                .WithMany(g => g.JoinRequests)
                .HasForeignKey(gjr => gjr.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupJoinRequest>()
                .HasOne(gjr => gjr.Requester)
                .WithMany(u => u.GroupJoinRequests)
                .HasForeignKey(gjr => gjr.RequesterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupJoinRequest>()
                .HasOne(gjr => gjr.ProcessedBy)
                .WithMany()
                .HasForeignKey(gjr => gjr.ProcessedById)
                .OnDelete(DeleteBehavior.SetNull);

            // ApplicationUserとGroupの関係（現在参加しているグループ）
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Group)
                .WithMany()
                .HasForeignKey(u => u.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            // GroupCodeの一意制約
            modelBuilder.Entity<Group>()
                .HasIndex(g => g.GroupCode)
                .IsUnique();

            // GroupMembershipの複合ユニーク制約（同じユーザーが同じグループに複数回参加できない）
            modelBuilder.Entity<GroupMembership>()
                .HasIndex(gm => new { gm.GroupId, gm.UserId })
                .IsUnique();
        }
    }
}