using ManualApp.Models;
using ManualApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ManualApp.Services
{
    public class CategoryService
    {
        private readonly ManualAppContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CategoryService(ManualAppContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            var userId = _currentUserService.UserId;
            return await _context.Categories
                .Where(c => c.OwnerId == null || c.OwnerId == userId) // 全ユーザー共通または自分のカテゴリー
                .OrderBy(c => c.IsDefault ? 0 : 1) // デフォルトカテゴリーを最初に
                .ThenBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            var userId = _currentUserService.UserId;
            return await _context.Categories
                .Where(c => c.CategoryId == id && (c.OwnerId == null || c.OwnerId == userId))
                .FirstOrDefaultAsync();
        }

        public async Task<Category> AddCategoryAsync(Category category)
        {
            category.OwnerId = _currentUserService.UserId;
            category.IsDefault = false;
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(Category category)
        {
            var userId = _currentUserService.UserId;
            var existingCategory = await _context.Categories.FindAsync(category.CategoryId);
            
            if (existingCategory == null)
                throw new InvalidOperationException("カテゴリーが見つかりません。");
            
            if (existingCategory.IsDefault)
                throw new InvalidOperationException("デフォルトカテゴリーは編集できません。");
            
            if (existingCategory.OwnerId != userId)
                throw new InvalidOperationException("このカテゴリーを編集する権限がありません。");

            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            
            await _context.SaveChangesAsync();
            return existingCategory;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var userId = _currentUserService.UserId;
            var category = await _context.Categories.FindAsync(id);
            
            if (category == null)
                return false;
            
            if (category.IsDefault)
                return false; // デフォルトカテゴリーは削除不可
            
            if (category.OwnerId != userId)
                return false; // 自分のカテゴリーでない場合は削除不可

            // このカテゴリを使用しているマニュアルがあるかチェック
            var hasManuals = await _context.Manuals.AnyAsync(m => m.CategoryId == id);
            if (hasManuals)
                return false; // マニュアルが存在する場合は削除不可

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanDeleteCategoryAsync(int id)
        {
            var userId = _currentUserService.UserId;
            var category = await _context.Categories.FindAsync(id);
            
            if (category == null || category.IsDefault || category.OwnerId != userId)
                return false;
                
            return !await _context.Manuals.AnyAsync(m => m.CategoryId == id);
        }

        public async Task<bool> CanEditCategoryAsync(int id)
        {
            var userId = _currentUserService.UserId;
            var category = await _context.Categories.FindAsync(id);
            
            if (category == null)
                return false;
                
            return !category.IsDefault && category.OwnerId == userId;
        }
    }
}
