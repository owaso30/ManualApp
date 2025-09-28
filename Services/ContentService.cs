using ManualApp.Models;
using ManualApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ManualApp.Services
{
    public class ContentService
    {
        private readonly ManualAppContext _context;
        private readonly S3Service _s3Service;
        private readonly ICurrentUserService _currentUserService;

        public ContentService(ManualAppContext context, S3Service s3Service, ICurrentUserService currentUserService)
        {
            _context = context;
            _s3Service = s3Service;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<Content>> GetContentsByManualIdAsync(int manualId)
        {
            return await _context.Contents
                .Include(c => c.Image)
                .Include(c => c.Creator)
                .Where(c => c.ManualId == manualId)
                .OrderBy(c => c.Order)
                .ToListAsync();
        }

        public async Task<Content?> GetContentByIdAsync(int id)
        {
            return await _context.Contents
                .Include(c => c.Image)
                .Include(c => c.Creator)
                .FirstOrDefaultAsync(c => c.ContentId == id);
        }

        public async Task<Content> AddContentAsync(Content content)
        {
            // ManualIdの存在確認
            var manualExists = await _context.Manuals.AnyAsync(m => m.ManualId == content.ManualId);
            if (!manualExists)
            {
                throw new InvalidOperationException($"指定されたマニュアル（ID: {content.ManualId}）が見つかりません。");
            }
            
            // 新しいコンテンツの順序を設定（最後に追加）
            var maxOrder = await _context.Contents
                .Where(c => c.ManualId == content.ManualId)
                .MaxAsync(c => (int?)c.Order) ?? 0;
            
            content.Order = maxOrder + 1;
            
            // 作成者IDを現在のユーザーに設定
            var userId = _currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("ユーザーIDが取得できません。ログイン状態を確認してください。");
            }
            
            content.CreatorId = userId;
            content.CreatedAt = DateTime.UtcNow;
            
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

            // S3上の画像ファイルを削除
            if (content.Image != null && !string.IsNullOrEmpty(content.Image.FilePath))
            {
                await _s3Service.DeleteFileIfExistsAsync(content.Image.FilePath);
            }

            // Contentを削除（CascadeでImageも自動削除される）
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
