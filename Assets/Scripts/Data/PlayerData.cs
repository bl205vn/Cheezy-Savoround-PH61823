using System;
using System.Collections.Generic;

[Serializable]
public class TimeValidationData
{
    public long LastVerifiedServerTicks;   // Lần cuối server xác nhận giờ
    public long LastVerifiedDeviceTicks;   // Giờ máy tại thời điểm đó (để tính offset)
    public long LastAppOpenDeviceTicks;    // Giờ máy lần mở app gần nhất (chống rollback offline)
    public int SuspiciousJumpCount;        // Đếm số lần phát hiện nhảy giờ bất thường
}

[Serializable]
public class PlayerData
{
    public int Gold;
    public List<string> UnlockedSkins;
    public string CurrentSkinId;
    public long LastDailyRewardTime; // Lưu dưới dạng UTC Ticks
    public int CurrentDailyRewardDay; // Ngày hiện tại trong chuỗi 7 ngày (0 - 6)
    public List<AchievementSaveData> Achievements;
    public GameSettings Settings;
    public List<int> BoostersOwned; // Lưu số lượng của 4 loại Booster
    public int CurrentLevel; // Cấp độ hiện tại của người chơi
    public LevelProgressData CurrentLevelProgress; // Lưu trạng thái ván chơi dang dở
    public int TotalScore;
    public int BestScore;
    public TimeValidationData TimeValidation;
    
    // Khởi tạo giá trị mặc định cho người chơi mới
    public PlayerData()
    {
        Gold = 0;
        CurrentLevel = 1;
        UnlockedSkins = new List<string>() { "plate_01" }; // Chỉ mở khoá skin mặc định cho người chơi mới
        CurrentSkinId = "plate_01";
        LastDailyRewardTime = 0;
        Achievements = new List<AchievementSaveData>();
        Settings = new GameSettings();
        BoostersOwned = new List<int>() { 0, 0, 0, 0 };
    }
}

[Serializable]
public struct AchievementSaveData
{
    public string Id;
    public int Progress;
    public bool IsClaimed;
}

[Serializable]
public class GridCellSaveData
{
    public int x;
    public int y;
    public int[] sliceTypes; // danh sách type của các slice trên đĩa, theo thứ tự index
}

[Serializable]
public class LevelProgressData
{
    public int levelId;
    public int currentScore; // Lưu tiến trình thanh điểm của ván chơi
    public List<GridCellSaveData> occupiedCells = new List<GridCellSaveData>();
    // Mỗi slot tray lưu một mảng các sliceType (nếu null là ô trống)
    public List<int[]> traySlots = new List<int[]>(); 
}
