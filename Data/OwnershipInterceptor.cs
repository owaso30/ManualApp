using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ManualApp.Services;
using ManualApp.Models;

namespace ManualApp.Data
{
    public class OwnershipInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _current;

        public OwnershipInterceptor(ICurrentUserService current) => _current = current;

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Stamp(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        private void Stamp(DbContext? ctx)
        {
            if (ctx == null || !_current.IsAuthenticated) return;

            foreach (var entry in ctx.ChangeTracker.Entries<Manual>())
            {
                if (entry.State == EntityState.Added && string.IsNullOrEmpty(entry.Entity.OwnerId))
                {
                    entry.Entity.OwnerId = _current.UserId!;
                }
            }
        }
    }
}