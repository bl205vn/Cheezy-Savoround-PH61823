using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý logic Daily Reward: kiểm tra thời gian, phát quà, cập nhật UI.
/// GẮN lên Canvas Daily (Canvas cha luôn bật). Panel con mới là thứ bật/tắt.
/// </summary>
public class DailyRewardManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private DailyRewardItemUI[] _dayItems; // Mảng 7 ô quà
    [SerializeField] private Button _claimButton;

    [Header("Reward Config (Dữ liệu từ ScriptableObject)")]
    [SerializeField] private DailyRewardConfig _config;

    private bool _canClaimToday = false;



    public void CheckDailyReward()
    {
        if (SaveLoadManager.Data == null || _config == null) return;

        long lastTimeTick = SaveLoadManager.Data.LastDailyRewardTime;
        DateTime lastRewardTime = new DateTime(lastTimeTick, DateTimeKind.Utc);
        DateTime now = DateTime.UtcNow;

        if (now.Date > lastRewardTime.Date)
        {
            _canClaimToday = true;

            // Nếu bỏ lỡ hơn 1 ngày, HOẶC đã nhận hết 7 ngày trước đó -> reset chuỗi về ngày 1
            if ((now.Date - lastRewardTime.Date).TotalDays > 1 || SaveLoadManager.Data.CurrentDailyRewardDay >= 7)
            {
                SaveLoadManager.Data.CurrentDailyRewardDay = 0;
            }
        }
        else
        {
            _canClaimToday = false;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_config == null) return;
        
        int currentDay = SaveLoadManager.Data.CurrentDailyRewardDay;

        for (int i = 0; i < 7; i++)
        {
            if (i < _dayItems.Length && i < _config.Rewards.Length)
            {
                // i nhỏ hơn currentDay -> Đã nhận
                bool isClaimed = i < currentDay;
                
                // i đúng bằng currentDay VÀ hôm nay có quyền nhận -> Đang chờ nhận (Cam)
                bool isCurrentDay = (i == currentDay && _canClaimToday);
                
                DailyRewardItem rewardItem = _config.Rewards[i];
                
                _dayItems[i].Setup(isClaimed, isCurrentDay, $"DAY {i + 1}", rewardItem.DisplayText, rewardItem.RewardIcon);
            }
        }

        if (_claimButton != null)
        {
            _claimButton.interactable = _canClaimToday;
        }
    }

    public void OnClaimButtonClicked()
    {
        if (!_canClaimToday || SaveLoadManager.Data == null || _config == null) return;

        int currentDay = SaveLoadManager.Data.CurrentDailyRewardDay;
        
        if (currentDay < _config.Rewards.Length)
        {
            DailyRewardItem reward = _config.Rewards[currentDay];
            
            switch (reward.RewardType)
            {
                case RewardType.Gold:
                    GiveGold(reward.GoldAmount);
                    break;
                    
                case RewardType.Booster:
                    GiveBooster(reward.BoosterType, reward.BoosterAmount);
                    break;

                case RewardType.Skin:
                    GiveSkin(reward.SkinId);
                    break;
                    
                case RewardType.Chest:
                    GiveChestContents(reward.ChestContents);
                    break;
            }
        }
        
        // Lưu lại thời gian và tăng ngày (Nếu lên 7 thì giữ nguyên số 7, để ngày mai CheckDailyReward mới reset về 0)
        SaveLoadManager.Data.LastDailyRewardTime = DateTime.UtcNow.Ticks;
        SaveLoadManager.Data.CurrentDailyRewardDay++;

        SaveLoadManager.Save();
        
        _canClaimToday = false;
        UpdateUI();
    }

    // ====== REWARD GIVERS (Tách riêng để dễ mở rộng) ======

    private void GiveGold(int amount)
    {
        SaveLoadManager.Data.Gold += amount;
        GoldDisplay.UpdateAll();
        Debug.Log($"[DailyReward] +{amount} Vàng");
    }

    private void GiveBooster(BoostButton.BoosterType type, int amount)
    {
        int index = (int)type;
        while (SaveLoadManager.Data.BoostersOwned.Count <= index)
        {
            SaveLoadManager.Data.BoostersOwned.Add(0);
        }
        SaveLoadManager.Data.BoostersOwned[index] += amount;
        BoostButton.UpdateAll();
        Debug.Log($"[DailyReward] +{amount} Booster [{type}]");
    }

    private void GiveSkin(string skinId)
    {
        if (string.IsNullOrEmpty(skinId)) return;
        
        if (!SaveLoadManager.Data.UnlockedSkins.Contains(skinId))
        {
            SaveLoadManager.Data.UnlockedSkins.Add(skinId);
            Debug.Log($"[DailyReward] Mở khóa Skin: {skinId}");
        }
        else
        {
            // Skin đã có rồi → Đền bù bằng vàng
            SaveLoadManager.Data.Gold += 100;
            GoldDisplay.UpdateAll();
            Debug.Log($"[DailyReward] Skin {skinId} đã có, đền bù 100 Vàng");
        }
    }

    /// <summary>
    /// Mở Rương: Duyệt qua toàn bộ danh sách ChestContents và phát quà tương ứng.
    /// </summary>
    private void GiveChestContents(List<RewardEntry> contents)
    {
        if (contents == null || contents.Count == 0) return;

        foreach (var entry in contents)
        {
            switch (entry.RewardType)
            {
                case RewardType.Gold:
                    GiveGold(entry.GoldAmount);
                    break;
                case RewardType.Booster:
                    GiveBooster(entry.BoosterType, entry.BoosterAmount);
                    break;
                case RewardType.Skin:
                    GiveSkin(entry.SkinId);
                    break;
            }
        }
        
        Debug.Log($"[DailyReward] Đã mở Rương chứa {contents.Count} phần quà!");
    }
}
