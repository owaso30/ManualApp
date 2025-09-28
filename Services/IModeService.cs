namespace ManualApp.Services
{
    public enum ViewMode
    {
        Personal = 0,    // 個人モード
        Group = 1        // グループモード
    }

    public interface IModeService
    {
        ViewMode CurrentMode { get; }
        Task SetModeAsync(ViewMode mode);
        Task<string?> GetCurrentOwnerIdAsync();
        Task<bool> CanSwitchToGroupModeAsync();
        Task<bool> IsGroupMemberAsync();
        Task<int?> GetCurrentGroupIdAsync();
        void ClearGroupCache();
    }
}
