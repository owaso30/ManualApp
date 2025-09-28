using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ManualApp.Services;
using ManualApp.Models;

namespace ManualApp.Data
{
    public class OwnershipInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _current;
        private readonly IModeService _modeService;

        public OwnershipInterceptor(ICurrentUserService current, IModeService modeService)
        {
            _current = current;
            _modeService = modeService;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Stamp(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        private void Stamp(DbContext? ctx)
        {
            if (ctx == null || !_current.IsAuthenticated) return;

            // ManualエンティティのOwnerIdを現在のモードに応じて設定
            foreach (var entry in ctx.ChangeTracker.Entries<Manual>())
            {
                if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.OwnerId))
                {
                    var currentOwnerId = _modeService.GetCurrentOwnerIdAsync().Result;
                    entry.Entity.OwnerId = currentOwnerId ?? _current.UserId!;
                }
            }

            // CategoryエンティティのOwnerIdを現在のモードに応じて設定
            foreach (var entry in ctx.ChangeTracker.Entries<Category>())
            {
                if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.OwnerId))
                {
                    var currentOwnerId = _modeService.GetCurrentOwnerIdAsync().Result;
                    entry.Entity.OwnerId = currentOwnerId ?? _current.UserId!;
                }
            }
        }
    }
}