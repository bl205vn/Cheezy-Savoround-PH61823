using System.Collections.Generic;
using UnityEngine;

public enum RewardType
{
    Gold,
    Booster,
    Skin,
    Chest
}

/// <summary>
/// Một phần thưởng đơn lẻ (Vàng, Booster, hoặc Skin).
/// Dùng cho cả quà hàng ngày lẫn nội dung bên trong Chest.
/// </summary>
[System.Serializable]
public class RewardEntry
{
    public RewardType RewardType;
    
    [Header("Gold")]
    public int GoldAmount; // Số vàng (chỉ dùng khi RewardType = Gold)
    
    [Header("Booster")]
    public BoostButton.BoosterType BoosterType; // Loại Booster
    public int BoosterAmount; // Số lượng Booster
    
    [Header("Skin")]
    public string SkinId; // ID skin để mở khóa (vd: "plate_05")
}

/// <summary>
/// Cấu hình quà cho 1 ngày trong Daily Reward.
/// - Gold: Chỉ cần điền GoldAmount.
/// - Booster: Chọn BoosterType + BoosterAmount.
/// - Skin: Nhập SkinId.
/// - Chest: Điền danh sách ChestContents (nhiều phần thưởng hỗn hợp).
/// </summary>
[System.Serializable]
public class DailyRewardItem
{
    public RewardType RewardType;
    public Sprite RewardIcon; // Icon hiển thị trên UI
    public string DisplayText; // Chữ hiển thị ở dưới (vd: "150", "TRASH CAN", "BIG REWARDS")

    [Header("--- Gold ---")]
    public int GoldAmount;

    [Header("--- Booster ---")]
    public BoostButton.BoosterType BoosterType;
    public int BoosterAmount;

    [Header("--- Skin ---")]
    public string SkinId; // vd: "plate_05"

    [Header("--- Chest (Nhiều quà hỗn hợp) ---")]
    public List<RewardEntry> ChestContents = new List<RewardEntry>();
}

[CreateAssetMenu(fileName = "DailyRewardConfig", menuName = "Cheezy Savoround/Daily Reward Config")]
public class DailyRewardConfig : ScriptableObject
{
    [Header("Danh sách quà 7 ngày")]
    public DailyRewardItem[] Rewards = new DailyRewardItem[7];
}
