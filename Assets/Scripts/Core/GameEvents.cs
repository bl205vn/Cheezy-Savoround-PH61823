using System;
using UnityEngine;

public static class GameEvents
{
    // Đặt đĩa thành công: Trả về đĩa được đặt và ô lưới nhận đĩa
    public static event Action<PizzaPlate, GridCell> OnPlatePlaced;
    public static void TriggerPlatePlaced(PizzaPlate plate, GridCell cell) => OnPlatePlaced?.Invoke(plate, cell);

    // Đặt đĩa thất bại: Trả về đĩa bị ném lại khay
    public static event Action<PizzaPlate> OnPlatePlaceFailed;
    public static void TriggerPlatePlaceFailed(PizzaPlate plate) => OnPlatePlaceFailed?.Invoke(plate);

    // Đĩa bánh nổ: Trả về loại pizza (int), điểm số cộng thêm (int), và vàng nhận được (int)
    public static event Action<int, int, int> OnPlateExploded;
    public static void TriggerPlateExploded(int pizzaType, int scoreAdded, int goldAdded) => OnPlateExploded?.Invoke(pizzaType, scoreAdded, goldAdded);

    // Đạt combo: Trả về số lượng chuỗi nổ (x2, x3...)
    public static event Action<int> OnComboAchieved;
    public static void TriggerComboAchieved(int comboCount) => OnComboAchieved?.Invoke(comboCount);

    // Thua cuộc
    public static event Action OnGameOver;
    public static void TriggerGameOver() => OnGameOver?.Invoke();

    // Thay đổi Skin đĩa
    public static event Action<string> OnSkinChanged;
    public static void TriggerSkinChanged(string newSkinId) => OnSkinChanged?.Invoke(newSkinId);

    // === Achievement System Events ===

    public static event Action OnLevelCompleted;
    public static void TriggerLevelCompleted() => OnLevelCompleted?.Invoke();

    public static event Action OnBoosterUsed;
    public static void TriggerBoosterUsed() => OnBoosterUsed?.Invoke();

    public static event Action<int> OnGoldAdded;
    public static void TriggerGoldAdded(int amount) => OnGoldAdded?.Invoke(amount);

    public static event Action<string> OnSkinUnlocked;
    public static void TriggerSkinUnlocked(string skinId) => OnSkinUnlocked?.Invoke(skinId);

    public static event Action<int> OnDailyLoginClaimed;
    public static void TriggerDailyLoginClaimed(int dayIndex) => OnDailyLoginClaimed?.Invoke(dayIndex);
}
