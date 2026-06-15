using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public static class ServerTimeProvider
{
    public static async Task<DateTime?> TryGetServerTimeAsync()
    {
        try
        {
            using (UnityWebRequest req = UnityWebRequest.Head("https://www.google.com"))
            {
                req.timeout = 5;
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (req.result != UnityWebRequest.Result.Success) return null;

                string dateHeader = req.GetResponseHeader("Date");
                if (DateTime.TryParse(dateHeader, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                    out DateTime serverTime))
                {
                    return serverTime;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ServerTime] Failed: {e.Message}");
        }
        return null;
    }
}

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

    [Header("Text Format")]
    [SerializeField] private string _dayPrefix = "DAY "; // Cho phép đổi chữ DAY thành chữ khác trên Inspector

    private bool _canClaimToday = false;

    private void Start()
    {
        // Gọi 1 lần lúc mới vào game để check giờ ngầm trước
        CheckDailyReward();
    }

    public async void CheckDailyReward()
    {
        if (SaveLoadManager.Data == null || _config == null) return;

        // Đảm bảo TimeValidation không bị null (với user cũ)
        if (SaveLoadManager.Data.TimeValidation == null)
        {
            SaveLoadManager.Data.TimeValidation = new TimeValidationData();
        }

        var tv = SaveLoadManager.Data.TimeValidation;
        DateTime deviceNow = DateTime.UtcNow;
        DateTime trustedNow;

        // --- Thử lấy server time ---
        DateTime? serverTime = await ServerTimeProvider.TryGetServerTimeAsync();

        if (serverTime.HasValue)
        {
            trustedNow = serverTime.Value;

            // Cập nhật mốc verified mới nhất: lưu cả server time VÀ device time tại thời điểm đó
            tv.LastVerifiedServerTicks = trustedNow.Ticks;
            tv.LastVerifiedDeviceTicks = deviceNow.Ticks;
            tv.SuspiciousJumpCount = 0; // Reset vì đã verify thành công
        }
        else
        {
            // --- OFFLINE: dùng device time, nhưng validate qua offset đã lưu ---
            if (tv.LastVerifiedServerTicks > 0)
            {
                // Tính thời gian đã trôi qua theo MÁY kể từ lần verify cuối
                long deviceElapsedTicks = deviceNow.Ticks - tv.LastVerifiedDeviceTicks;

                // Ước lượng "giờ thật" = giờ server đã verify + thời gian trôi qua theo máy
                DateTime estimatedNow = new DateTime(tv.LastVerifiedServerTicks + deviceElapsedTicks, DateTimeKind.Utc);

                // So sánh device time hiện tại với estimate
                TimeSpan diff = estimatedNow - deviceNow;

                if (diff.TotalMinutes > 5) // Threshold tránh false positive do lệch timezone
                {
                    Debug.LogWarning($"[DailyReward] Phát hiện lùi giờ offline: lệch {diff.TotalHours:F1}h");
                    tv.SuspiciousJumpCount++;
                    trustedNow = estimatedNow; // Dùng estimate, không cho qua ngày mới bằng cách lùi giờ
                }
                else if (diff.TotalMinutes < -5)
                {
                    // Chỉnh TIẾN giờ offline -> Tăng count nghi ngờ, nhưng vẫn phải dùng device time vì không có căn cứ
                    tv.SuspiciousJumpCount++;
                    trustedNow = deviceNow;
                }
                else
                {
                    trustedNow = deviceNow;
                }

                // Cập nhật baseline để lần check sau dùng deviceElapsed từ điểm này
                tv.LastVerifiedDeviceTicks = deviceNow.Ticks;
                tv.LastVerifiedServerTicks = trustedNow.Ticks;
            }
            else
            {
                // Chưa từng verify server lần nào (lần đầu offline) -> tin device
                trustedNow = deviceNow;
                tv.LastVerifiedDeviceTicks = deviceNow.Ticks;
                tv.LastVerifiedServerTicks = deviceNow.Ticks;
            }
        }

        // --- Áp dụng logic claim dùng trustedNow thay vì DateTime.UtcNow ---
        long lastTimeTick = SaveLoadManager.Data.LastDailyRewardTime;
        DateTime lastRewardTime = new DateTime(lastTimeTick, DateTimeKind.Utc);

        if (trustedNow.Date > lastRewardTime.Date)
        {
            _canClaimToday = true;

            if ((trustedNow.Date - lastRewardTime.Date).TotalDays > 1 || SaveLoadManager.Data.CurrentDailyRewardDay >= 7)
            {
                SaveLoadManager.Data.CurrentDailyRewardDay = 0;
            }

            // Nếu phát hiện quá nhiều lần nhảy giờ bất thường -> Phạt reset chuỗi
            if (tv.SuspiciousJumpCount >= 3)
            {
                Debug.LogWarning("[DailyReward] Quá nhiều lần phát hiện chỉnh giờ bất thường, reset chuỗi reward.");
                SaveLoadManager.Data.CurrentDailyRewardDay = 0;
                tv.SuspiciousJumpCount = 0; // Phạt xong thì tha
            }
        }
        else
        {
            _canClaimToday = false;
        }

        // Bắt buộc Save lại những thay đổi về TimeValidation
        SaveLoadManager.Save();
        
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
                
                _dayItems[i].Setup(isClaimed, isCurrentDay, $"{_dayPrefix}{i + 1}", rewardItem.DisplayText, rewardItem.RewardIcon);
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
        
        // Cập nhật bằng trusted time của server (nếu nãy offline thì nó lấy offset estimate)
        // Vì CheckDailyReward đã lưu LastVerifiedServerTicks chuẩn nhất.
        SaveLoadManager.Data.LastDailyRewardTime = SaveLoadManager.Data.TimeValidation.LastVerifiedServerTicks;
        SaveLoadManager.Data.CurrentDailyRewardDay++;
        
        GameEvents.TriggerDailyLoginClaimed(SaveLoadManager.Data.CurrentDailyRewardDay);

        SaveLoadManager.Save();
        
        _canClaimToday = false;
        UpdateUI();
    }

    // ====== REWARD GIVERS ======

    private void GiveGold(int amount)
    {
        SaveLoadManager.Data.Gold += amount;
        GameEvents.TriggerGoldAdded(amount);
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
            GameEvents.TriggerSkinUnlocked(skinId);
            Debug.Log($"[DailyReward] Mở khóa Skin: {skinId}");
        }
        else
        {
            SaveLoadManager.Data.Gold += 100;
            GameEvents.TriggerGoldAdded(100);
            GoldDisplay.UpdateAll();
            Debug.Log($"[DailyReward] Skin {skinId} đã có, đền bù 100 Vàng");
        }
    }

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
