using ManualApp.Data;
using ManualApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ManualApp.Services
{
    public class ModeService : IModeService
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly ManualAppContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string MODE_COOKIE_KEY = "CurrentViewMode";

        public ModeService(ICurrentUserService currentUserService, ManualAppContext context, IHttpContextAccessor httpContextAccessor)
        {
            _currentUserService = currentUserService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public ViewMode CurrentMode
        {
            get
            {
                try
                {
                    var request = _httpContextAccessor.HttpContext?.Request;
                    if (request == null)
                        return ViewMode.Group;

                    var cookieValue = request.Cookies[MODE_COOKIE_KEY];
                    if (string.IsNullOrEmpty(cookieValue))
                        return ViewMode.Group; // デフォルトはグループモード

                    if (Enum.TryParse<ViewMode>(cookieValue, out var mode))
                        return mode;

                    return ViewMode.Group;
                }
                catch (Exception)
                {
                    // Cookieエラーの場合はデフォルト値を返す
                    return ViewMode.Group;
                }
            }
        }

        public async Task SetModeAsync(ViewMode mode)
        {
            try
            {
                // サーバーサイドでCookieを設定
                var response = _httpContextAccessor.HttpContext?.Response;
                if (response != null)
                {
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = false, // JavaScriptからアクセス可能にする
                        Secure = false, // 開発環境ではfalse
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTime.Now.AddDays(30) // 30日間有効
                    };
                    
                    response.Cookies.Append(MODE_COOKIE_KEY, mode.ToString(), cookieOptions);
                }
            }
            catch (Exception)
            {
                // Cookieエラーは無視（デフォルト値を使用）
            }
            
            await Task.CompletedTask; // async メソッドのため
        }

        public async Task<string?> GetCurrentOwnerIdAsync()
        {
            if (!_currentUserService.IsAuthenticated)
                return null;

            var userId = _currentUserService.UserId;
            if (userId == null)
                return null;

            // 現在のモードを取得
            var currentMode = CurrentMode;
            
            switch (currentMode)
            {
                case ViewMode.Personal:
                    return userId;

                case ViewMode.Group:
                    // グループモードの場合でも、グループに所属していない場合は個人モードとして扱う
                    var isGroupMember = await CanSwitchToGroupModeAsync();
                    if (!isGroupMember)
                    {
                        return userId;
                    }
                    
                    // グループモードの場合は、グループIDを文字列として返す
                    var groupId = await GetCurrentGroupIdAsync();
                    return groupId?.ToString();

                default:
                    return userId;
            }
        }

        private bool? _cachedIsGroupMember = null;
        private int? _cachedGroupId = null;
        private string? _lastUserId = null;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public async Task<bool> CanSwitchToGroupModeAsync()
        {
            try
            {
                if (!_currentUserService.IsAuthenticated)
                    return false;

                var userId = _currentUserService.UserId;
                if (userId == null)
                    return false;

                // ユーザーが変わった場合はキャッシュをクリア
                if (_lastUserId != userId)
                {
                    _cachedIsGroupMember = null;
                    _cachedGroupId = null;
                    _lastUserId = userId;
                }

                // キャッシュがない場合のみデータベースアクセス（並行実行を防ぐ）
                if (_cachedIsGroupMember == null)
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        // 再度チェック（他のスレッドが既にロードした可能性がある）
                        if (_cachedIsGroupMember == null)
                        {
                            await LoadUserGroupInfoAsync(userId);
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }

                return _cachedIsGroupMember ?? false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task LoadUserGroupInfoAsync(string userId)
        {
            try
            {
                // 1回のクエリでユーザーとグループ情報を取得
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new { UserGroupId = u.GroupId, GroupId = u.Group.GroupId })
                    .FirstOrDefaultAsync();

                _cachedIsGroupMember = user?.UserGroupId != null;
                _cachedGroupId = user?.UserGroupId;
            }
            catch (Exception)
            {
                _cachedIsGroupMember = false;
                _cachedGroupId = null;
            }
        }

        public async Task<bool> IsGroupMemberAsync()
        {
            return await CanSwitchToGroupModeAsync();
        }

        public async Task<int?> GetCurrentGroupIdAsync()
        {
            try
            {
                if (!_currentUserService.IsAuthenticated)
                    return null;

                var userId = _currentUserService.UserId;
                if (userId == null)
                    return null;

                // ユーザーが変わった場合はキャッシュをクリア
                if (_lastUserId != userId)
                {
                    _cachedIsGroupMember = null;
                    _cachedGroupId = null;
                    _lastUserId = userId;
                }

                // キャッシュがない場合のみデータベースアクセス（並行実行を防ぐ）
                if (_cachedGroupId == null && _cachedIsGroupMember == null)
                {
                    await _semaphore.WaitAsync();
                    try
                    {
                        // 再度チェック（他のスレッドが既にロードした可能性がある）
                        if (_cachedGroupId == null && _cachedIsGroupMember == null)
                        {
                            await LoadUserGroupInfoAsync(userId);
                        }
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }

                return _cachedGroupId;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// グループ情報のキャッシュをクリアします
        /// </summary>
        public void ClearGroupCache()
        {
            _cachedIsGroupMember = null;
            _cachedGroupId = null;
            _lastUserId = null;
        }
    }
}
