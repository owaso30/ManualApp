using ManualApp.Models;
using ManualApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ManualApp.Services
{
    public class ManualService
    {
        private readonly ManualAppContext _context;

        public ManualService(ManualAppContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Manual>> GetAllManualsAsync()
        {
            return await _context.Manuals.ToListAsync();
        }

        public async Task<IEnumerable<Manual>> GetManualsByUserAsync(string userId)
        {
            return await _context.Manuals
                .Include(m => m.Category)
                .Where(m => m.OwnerId == userId)
                .OrderBy(m => m.Title)
                .ToListAsync();
        }

        public async Task AddManualAsync(Manual manual)
        {
            _context.Manuals.Add(manual);
            await _context.SaveChangesAsync();
        }

        public async Task<Manual?> GetManualByIdAsync(int id)
        {
            return await _context.Manuals.FindAsync(id);
        }

        public async Task UpdateManualAsync(Manual manual)
        {
            _context.Manuals.Update(manual);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteManualAsync(int id)
        {
            var manual = await _context.Manuals.FindAsync(id);
            if (manual != null)
            {
                _context.Manuals.Remove(manual);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
