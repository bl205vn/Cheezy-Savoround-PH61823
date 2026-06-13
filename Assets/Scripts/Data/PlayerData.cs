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
    
    // Khởi tạo giá trị mặc định cho người chơi mới
    public PlayerData()
    {
        Gold = 0;
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
