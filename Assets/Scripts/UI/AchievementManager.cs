using UnityEngine;
using System.Linq;

/// <summary>
/// Quản lý lắng nghe các Event từ Game để cộng tiến trình thành tựu, 
/// tự động trả thưởng, lưu save file và cập nhật UI.
/// </summary>
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    [SerializeField] private AchievementConfig _config;
    
    [Header("UI References")]
    [SerializeField] private AchievementItemUI[] _uiItems; // Kéo 5 object "Thanhtuu" vào đây

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Đăng ký lắng nghe sự kiện
        GameEvents.OnPlateExploded += HandlePlateExploded;
        GameEvents.OnLevelCompleted += HandleLevelCompleted;
        GameEvents.OnBoosterUsed += HandleBoosterUsed;
        GameEvents.OnGoldAdded += HandleGoldAdded;
        GameEvents.OnSkinUnlocked += HandleSkinUnlocked;
        GameEvents.OnDailyLoginClaimed += HandleDailyLoginClaimed;
    }

    private void OnDisable()
    {
        // Hủy đăng ký khi tắt
        GameEvents.OnPlateExploded -= HandlePlateExploded;
        GameEvents.OnLevelCompleted -= HandleLevelCompleted;
        GameEvents.OnBoosterUsed -= HandleBoosterUsed;
        GameEvents.OnGoldAdded -= HandleGoldAdded;
        GameEvents.OnSkinUnlocked -= HandleSkinUnlocked;
        GameEvents.OnDailyLoginClaimed -= HandleDailyLoginClaimed;
    }

    private void Start()
    {
        RefreshUI();
    }

    /// <summary>
    /// Đồng bộ dữ liệu hiện tại lên giao diện
    /// </summary>
    public void RefreshUI()
    {
        if (_config == null || _uiItems == null || SaveLoadManager.Data == null) return;

        for (int i = 0; i < _uiItems.Length; i++)
        {
            if (i >= _config.Achievements.Count)
            {
                // Nếu số slot UI lớn hơn số thành tựu cấu hình thì ẩn bớt
                _uiItems[i].gameObject.SetActive(false);
                continue;
            }

            _uiItems[i].gameObject.SetActive(true);
            var configItem = _config.Achievements[i];
            var saveData = GetOrCreateSaveData(configItem.Id);
            
            _uiItems[i].Setup(configItem, saveData);
        }
    }

    private AchievementSaveData GetOrCreateSaveData(string id)
    {
        var data = SaveLoadManager.Data.Achievements.FirstOrDefault(a => a.Id == id);
        if (string.IsNullOrEmpty(data.Id))
        {
            data = new AchievementSaveData { Id = id, Progress = 0, IsClaimed = false };
            SaveLoadManager.Data.Achievements.Add(data);
        }
        return data;
    }

    private void UpdateProgress(AchievementType type, int amount)
    {
        if (_config == null || SaveLoadManager.Data == null) return;

        bool isChanged = false;

        for (int i = 0; i < _config.Achievements.Count; i++)
        {
            var configItem = _config.Achievements[i];
            if (configItem.Type != type) continue; // Không đúng loại sự kiện đang xét

            var saveData = GetOrCreateSaveData(configItem.Id);
            if (saveData.IsClaimed) continue; // Đã nhận quà rồi thì bỏ qua không tăng nữa

            // Tìm index để cập nhật lại vì struct là value type
            int index = SaveLoadManager.Data.Achievements.FindIndex(a => a.Id == configItem.Id);
            
            // Xử lý riêng biệt với UnlockCakes (cần đếm tổng Skin hiện có)
            if (type == AchievementType.UnlockCakes)
            {
                // Đếm số lượng skin người chơi đang có (trừ skin mặc định plate_01)
                int unlockedCount = SaveLoadManager.Data.UnlockedSkins.Count;
                if (unlockedCount > saveData.Progress)
                {
                    saveData.Progress = unlockedCount;
                }
            }
            else
            {
                // Cộng dồn tiến trình
                saveData.Progress += amount;
            }

            // Kiểm tra đạt mốc nhận thưởng
            if (saveData.Progress >= configItem.TargetGoal && !saveData.IsClaimed)
            {
                saveData.Progress = configItem.TargetGoal;
                // BỎ AUTO-CLAIM THEO YÊU CẦU CỦA USER
                // saveData.IsClaimed = true;
                // GiveReward(configItem);
                // ShowUnlockPopup(configItem);
            }

            // Lưu ngược lại vào Data (chỉ lưu trên RAM)
            SaveLoadManager.Data.Achievements[index] = saveData;
            isChanged = true;
        }

        if (isChanged)
        {
            // TỐI ƯU HIỆU NĂNG:
            // Không gọi SaveLoadManager.Save() ở đây nữa để tránh việc nổ 1 đĩa lưu file 1 lần gây giật lag (Giảm thiểu I/O).
            // Dữ liệu sẽ được tự động lưu vào ổ cứng khi người chơi đóng game/ẩn game (Hệ thống Meta Save hôm qua đã lo việc này)
            // Hoặc lưu khi người chơi bấm nút Nhận thưởng (ClaimReward).
        }
    }

    /// <summary>
    /// Được gọi khi người chơi bấm nút "Nhận" trên UI
    /// </summary>
    public void ClaimReward(string achievementId)
    {
        var configItem = _config.Achievements.FirstOrDefault(a => a.Id == achievementId);
        if (configItem == null) return;

        int index = SaveLoadManager.Data.Achievements.FindIndex(a => a.Id == achievementId);
        if (index == -1) return;

        var saveData = SaveLoadManager.Data.Achievements[index];
        if (saveData.Progress >= configItem.TargetGoal && !saveData.IsClaimed)
        {
            saveData.IsClaimed = true;
            SaveLoadManager.Data.Achievements[index] = saveData;
            
            GiveReward(configItem);
            ShowUnlockPopup(configItem);
            
            SaveLoadManager.Save();
            RefreshUI();
        }
    }

    private void GiveReward(AchievementItem configItem)
    {
        switch (configItem.RewardType)
        {
            case RewardType.Gold:
                SaveLoadManager.Data.Gold += configItem.RewardAmount;
                break;
            case RewardType.Booster:
                int bIndex = (int)configItem.BoosterType;
                if (bIndex >= 0 && bIndex < SaveLoadManager.Data.BoostersOwned.Count)
                {
                    SaveLoadManager.Data.BoostersOwned[bIndex] += configItem.RewardAmount;
                }
                break;
            case RewardType.Skin:
                if (!SaveLoadManager.Data.UnlockedSkins.Contains(configItem.SkinId))
                {
                    SaveLoadManager.Data.UnlockedSkins.Add(configItem.SkinId);
                }
                else
                {
                    // Đền bù 100 vàng nếu đã có Skin này
                    SaveLoadManager.Data.Gold += 100;
                }
                break;
        }
    }

    private void ShowUnlockPopup(AchievementItem item)
    {
        // TODO: Kết nối với UI hiển thị Popup thông báo
        // Hiện tại tạm Log ra Console để xác thực trước theo Task 4.2
        Debug.Log($"<color=orange>🏆 [THÀNH TỰU] Đã mở khóa: {item.Description} - Nhận phần thưởng!</color>");
    }

    private bool _boosterUsedThisLevel = false;

    // --- Các hàm Handler lắng nghe từ GameEvents ---
    private void HandlePlateExploded(int pizzaType, int scoreAdded, int goldAdded)
    {
        // Theo dõi số vàng thu thập được từ nổ đĩa (kể cả vàng từ combo)
        if (goldAdded > 0)
        {
            UpdateProgress(AchievementType.CollectCoins, goldAdded);
        }

        // Loại bỏ cái fake event tạo âm thanh combo (pizzaType == -1) để không cộng sai số đĩa
        if (pizzaType == -1) return; 
        
        UpdateProgress(AchievementType.MatchPlates, 1);
    }

    private void HandleBoosterUsed()
    {
        _boosterUsedThisLevel = true;
    }

    private void HandleLevelCompleted()
    {
        if (!_boosterUsedThisLevel)
        {
            UpdateProgress(AchievementType.LevelCompletedNoBooster, 1);
        }
        _boosterUsedThisLevel = false; // Reset cờ cho màn sau
    }

    private void HandleGoldAdded(int amount) => UpdateProgress(AchievementType.CollectCoins, amount);
    private void HandleSkinUnlocked(string skinId) => UpdateProgress(AchievementType.UnlockCakes, 1);
    private void HandleDailyLoginClaimed(int dayIndex) => UpdateProgress(AchievementType.LoginConsecutiveDays, 1);
}
