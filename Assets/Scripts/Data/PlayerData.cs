using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public int Gold;
    public List<string> UnlockedSkins;
    public string CurrentSkinId;
    public long LastDailyRewardTime; // Lưu dưới dạng UTC Ticks
    public List<AchievementSaveData> Achievements;
    public GameSettings Settings;
    public List<int> BoostersOwned; // Lưu số lượng của 4 loại Booster
    public int CurrentLevel; // Cấp độ hiện tại của người chơi
    public LevelProgressData CurrentLevelProgress; // Lưu trạng thái ván chơi dang dở
    
    // Khởi tạo giá trị mặc định cho người chơi mới
    public PlayerData()
    {
        Gold = 0;
        CurrentLevel = 1;
        UnlockedSkins = new List<string>() { "plate_01", "plate_02", "plate_03", "plate_04", "plate_05", "plate_06" };
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
}

[Serializable]
public class GridCellSaveData
{
    public int x;
    public int y;
    public int[] sliceTypes; // danh sách type của các slice trên đĩa, theo thứ tự index
    public int priority;
}

[Serializable]
public class LevelProgressData
{
    public int levelId;
    public List<GridCellSaveData> occupiedCells = new List<GridCellSaveData>();
    // Mỗi slot tray lưu một mảng các sliceType (nếu null là ô trống)
    public List<int[]> traySlots = new List<int[]>(); 
}
