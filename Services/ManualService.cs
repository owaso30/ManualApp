using ManualApp.Models;
using ManualApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ManualApp.Services
{
    public class ManualService
    {
        private readonly ManualAppContext _context;
        private readonly IModeService _modeService;
        private readonly ICurrentUserService _currentUserService;
        private readonly S3Service _s3Service;

        public ManualService(ManualAppContext context, IModeService modeService, ICurrentUserService currentUserService, S3Service s3Service)
        {
            _context = context;
            _modeService = modeService;
            _currentUserService = currentUserService;
            _s3Service = s3Service;
        }

        public async Task<IEnumerable<Manual>> GetAllManualsAsync()
        {
            return await _context.Manuals.ToListAsync();
        }

            public async Task<IEnumerable<Manual>> GetManualsByUserAsync(string userId)
            {
                try
                {
                    var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
                    return await _context.Manuals
                        .Include(m => m.Category)
                        .Include(m => m.Owner)
                        .Include(m => m.Creator)
                        .Where(m => m.OwnerId == currentOwnerId)
                        .OrderBy(m => m.Title)
                        .ToListAsync();
                }
                catch (Exception)
                {
                    // データベースエラーの場合は空のリストを返す
                    return new List<Manual>();
                }
            }

        public async Task AddManualAsync(Manual manual)
        {
            // 現在のモードに応じてOwnerIdを設定
            var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
            manual.OwnerId = currentOwnerId;
            
            // 作成者IDを現在のユーザーに設定
            var currentUserId = _currentUserService.UserId;
            manual.CreatorId = currentUserId;
            
            _context.Manuals.Add(manual);
            await _context.SaveChangesAsync();
        }

        public async Task<Manual?> GetManualByIdAsync(int id)
        {
            try
            {
                var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
                return await _context.Manuals
                    .Include(m => m.Category)
                    .Include(m => m.Owner)
                    .Include(m => m.Creator)
                    .Where(m => m.ManualId == id && m.OwnerId == currentOwnerId)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                // データベースエラーの場合はnullを返す
                return null;
            }
        }

        public async Task UpdateManualAsync(Manual manual)
        {
            _context.Manuals.Update(manual);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteManualAsync(int id)
        {
            var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
            var manual = await _context.Manuals
                .Include(m => m.Contents)
                    .ThenInclude(c => c.Image)
                .Where(m => m.ManualId == id && m.OwnerId == currentOwnerId)
                .FirstOrDefaultAsync();
                
            if (manual != null)
            {
                // マニュアルに含まれるすべての画像をS3から削除
                foreach (var content in manual.Contents)
                {
                    if (content.Image != null && !string.IsNullOrEmpty(content.Image.FilePath))
                    {
                        await _s3Service.DeleteFileIfExistsAsync(content.Image.FilePath);
                    }
                }

                // Manualを削除（CascadeでContentとImageも自動削除される）
                _context.Manuals.Remove(manual);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> TransferManualToGroupAsync(int manualId)
        {
            try
            {
                var currentUserId = _currentUserService.UserId;
                if (currentUserId == null)
                    return false;

                // ユーザーがグループに所属しているかチェック
                var user = await _context.Users
                    .Where(u => u.Id == currentUserId)
                    .Select(u => new { u.GroupId })
                    .FirstOrDefaultAsync();

                if (user?.GroupId == null)
                    return false; // グループに所属していない

                // マニュアルを取得して所有権を確認
                var manual = await _context.Manuals.FindAsync(manualId);
                if (manual == null)
                    return false;

                // 個人所有のマニュアルのみグループ所有に変更可能
                if (manual.OwnerId != currentUserId)
                    return false;

                // 所属グループIDをそのまま設定
                manual.OwnerId = user.GroupId.ToString();
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
