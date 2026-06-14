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

    // Đĩa bánh nổ: Trả về loại pizza (int) và điểm số cộng thêm
    public static event Action<int, int> OnPlateExploded;
    public static void TriggerPlateExploded(int pizzaType, int scoreAdded) => OnPlateExploded?.Invoke(pizzaType, scoreAdded);

    // Đạt combo: Trả về số lượng chuỗi nổ (x2, x3...)
    public static event Action<int> OnComboAchieved;
    public static void TriggerComboAchieved(int comboCount) => OnComboAchieved?.Invoke(comboCount);

    // Thua cuộc
    public static event Action OnGameOver;
    public static void TriggerGameOver() => OnGameOver?.Invoke();
}
