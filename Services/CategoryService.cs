using ManualApp.Models;
using ManualApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ManualApp.Services
{
    public class CategoryService
    {
        private readonly ManualAppContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IModeService _modeService;

        public CategoryService(ManualAppContext context, ICurrentUserService currentUserService, IModeService modeService)
        {
            _context = context;
            _currentUserService = currentUserService;
            _modeService = modeService;
        }

            public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
            {
                try
                {
                    var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
                    var categories = await _context.Categories
                        .Include(c => c.Owner)
                        .Include(c => c.Creator)
                        .Where(c => c.OwnerId == null || c.OwnerId == currentOwnerId) // 全ユーザー共通または現在のモードのカテゴリー
                        .OrderBy(c => c.IsDefault ? 0 : 1) // デフォルトカテゴリーを最初に
                        .ThenBy(c => c.Name)
                        .ToListAsync();

                    // 各カテゴリーのマニュアル数を現在のユーザーがアクセスできるもののみでカウント
                    foreach (var category in categories)
                    {
                        category.ManualCount = await _context.Manuals
                            .Where(m => m.CategoryId == category.CategoryId && m.OwnerId == currentOwnerId)
                            .CountAsync();
                        
                        // マニュアルリストは空にしておく（実際のマニュアルオブジェクトは含めない）
                        category.Manuals = new List<Manual>();
                    }

                    return categories;
                }
                catch (Exception)
                {
                    // データベースエラーの場合は空のリストを返す
                    return new List<Category>();
                }
            }

            public async Task<Category?> GetCategoryByIdAsync(int id)
            {
                try
                {
                    var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
                    return await _context.Categories
                        .Where(c => c.CategoryId == id && (c.OwnerId == null || c.OwnerId == currentOwnerId))
                        .FirstOrDefaultAsync();
                }
                catch (Exception)
                {
                    // データベースエラーの場合はnullを返す
                    return null;
                }
            }

        public async Task<Category> AddCategoryAsync(Category category)
        {
            var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
            category.OwnerId = currentOwnerId;
            category.IsDefault = false;
            
            // 作成者IDを現在のユーザーに設定
            var currentUserId = _currentUserService.UserId;
            category.CreatorId = currentUserId;
            
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
            var existingCategory = await _context.Categories.FindAsync(category.CategoryId);
            
            if (existingCategory == null)
                throw new InvalidOperationException("カテゴリーが見つかりません。");
            
            if (existingCategory.IsDefault)
                throw new InvalidOperationException("デフォルトカテゴリーは編集できません。");
            
            if (existingCategory.OwnerId != currentOwnerId)
                throw new InvalidOperationException("このカテゴリーを編集する権限がありません。");

            existingCategory.Name = category.Name;
            
            await _context.SaveChangesAsync();
            return existingCategory;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
            var category = await _context.Categories.FindAsync(id);
            
            if (category == null)
                return false;
            
            if (category.IsDefault)
                return false; // デフォルトカテゴリーは削除不可
            
            if (category.OwnerId != currentOwnerId)
                return false; // 現在のモードのカテゴリーでない場合は削除不可

            // デフォルトカテゴリーを取得
            var defaultCategory = await _context.Categories
                .Where(c => c.IsDefault)
                .FirstOrDefaultAsync();
            
            if (defaultCategory == null)
                return false; // デフォルトカテゴリーが存在しない場合は削除不可

            // このカテゴリを使用しているマニュアルをデフォルトカテゴリーに変更
            var manualsToUpdate = await _context.Manuals
                .Where(m => m.CategoryId == id)
                .ToListAsync();
            
            foreach (var manual in manualsToUpdate)
            {
                manual.CategoryId = defaultCategory.CategoryId;
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanDeleteCategoryAsync(int id)
        {
            var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
            var category = await _context.Categories.FindAsync(id);
            
            if (category == null || category.IsDefault || category.OwnerId != currentOwnerId)
                return false;
                
            // デフォルトカテゴリーが存在するかチェック
            var hasDefaultCategory = await _context.Categories
                .AnyAsync(c => c.IsDefault);
                
            return hasDefaultCategory;
        }

        public async Task<bool> CanEditCategoryAsync(int id)
        {
            var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
            var category = await _context.Categories.FindAsync(id);
            
            if (category == null)
                return false;
                
            return !category.IsDefault && category.OwnerId == currentOwnerId;
        }

        public async Task<bool> TransferCategoryToGroupAsync(int categoryId)
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

                // カテゴリーを取得して所有権を確認
                var category = await _context.Categories.FindAsync(categoryId);
                if (category == null || category.IsDefault)
                    return false;

                // 個人所有のカテゴリーのみグループ所有に変更可能
                if (category.OwnerId != currentUserId)
                    return false;

                // 所属グループIDをそのまま設定
                category.OwnerId = user.GroupId.ToString();
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
