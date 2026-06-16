using UnityEngine;
using System.Collections.Generic;

public enum AchievementType
{
    MatchPlates,              // Successfully match 50 plates
    UnlockCakes,              // Unlock 5 new types of cakes (Skins)
    LevelCompletedNoBooster,  // Complete a level without using assistance
    CollectCoins,             // Collect a total of 1,000 Coins
    LoginConsecutiveDays      // Log in for 7 consecutive days
}

[System.Serializable]
public class AchievementItem
{
    public string Id;
    public string Description;
    public AchievementType Type;
    public int TargetGoal;
    
    [Header("Reward")]
    public RewardType RewardType; 
    public int RewardAmount; // Dùng cho Gold hoặc Booster
    public BoostButton.BoosterType BoosterType; // Dùng cho Booster
    public string SkinId; // Dùng cho Skin
    public Sprite RewardIcon;
}

[CreateAssetMenu(fileName = "AchievementConfig", menuName = "Cheezy Savoround/Achievement Config")]
public class AchievementConfig : ScriptableObject
{
    [Header("Danh sách 5 thành tựu (tương ứng với UI)")]
    public List<AchievementItem> Achievements = new List<AchievementItem>();
}
