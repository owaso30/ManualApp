using ManualApp.Models;
using ManualApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ManualApp.Services
{
    public class ContentService
    {
        private readonly ManualAppContext _context;

        public ContentService(ManualAppContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Content>> GetContentsByManualIdAsync(int manualId)
        {
            return await _context.Contents
                .Include(c => c.Image)
                .Where(c => c.ManualId == manualId)
                .OrderBy(c => c.Order)
                .ToListAsync();
        }

        public async Task<Content?> GetContentByIdAsync(int id)
        {
            return await _context.Contents
                .Include(c => c.Image)
                .FirstOrDefaultAsync(c => c.ContentId == id);
        }

        public async Task<Content> AddContentAsync(Content content)
        {
            // 新しいコンテンツの順序を設定（最後に追加）
            var maxOrder = await _context.Contents
                .Where(c => c.ManualId == content.ManualId)
                .MaxAsync(c => (int?)c.Order) ?? 0;
            
            content.Order = maxOrder + 1;
            
            _context.Contents.Add(content);
            await _context.SaveChangesAsync();
            return content;
        }

        public async Task<Content> UpdateContentAsync(Content content)
        {
            _context.Contents.Update(content);
            await _context.SaveChangesAsync();
            return content;
        }

        public async Task<bool> DeleteContentAsync(int id)
        {
            var content = await _context.Contents
                .Include(c => c.Image)
                .FirstOrDefaultAsync(c => c.ContentId == id);
            
            if (content == null)
                return false;

            // 画像も削除
            if (content.Image != null)
            {
                _context.Images.Remove(content.Image);
            }

            _context.Contents.Remove(content);
            await _context.SaveChangesAsync();
            
            // 削除後に順序を再調整
            await ReorderContentsAsync(content.ManualId);
            return true;
        }

        public async Task<bool> MoveContentUpAsync(int contentId)
        {
            var content = await _context.Contents.FindAsync(contentId);
            if (content == null)
                return false;

            var previousContent = await _context.Contents
                .Where(c => c.ManualId == content.ManualId && c.Order < content.Order)
                .OrderByDescending(c => c.Order)
                .FirstOrDefaultAsync();

            if (previousContent == null)
                return false; // 既に最上位

            // 順序を入れ替え
            var tempOrder = content.Order;
            content.Order = previousContent.Order;
            previousContent.Order = tempOrder;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MoveContentDownAsync(int contentId)
        {
            var content = await _context.Contents.FindAsync(contentId);
            if (content == null)
                return false;

            var nextContent = await _context.Contents
                .Where(c => c.ManualId == content.ManualId && c.Order > content.Order)
                .OrderBy(c => c.Order)
                .FirstOrDefaultAsync();

            if (nextContent == null)
                return false; // 既に最下位

            // 順序を入れ替え
            var tempOrder = content.Order;
            content.Order = nextContent.Order;
            nextContent.Order = tempOrder;

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task ReorderContentsAsync(int manualId)
        {
            var contents = await _context.Contents
                .Where(c => c.ManualId == manualId)
                .OrderBy(c => c.Order)
                .ToListAsync();

            for (int i = 0; i < contents.Count; i++)
            {
                contents[i].Order = i + 1;
            }

            await _context.SaveChangesAsync();
        }
    }
}
