using UnityEngine;
using TMPro;

public class BoostButton : MonoBehaviour
{
    public enum BoosterType
    {
        Cutter = 0,
        Sauce = 1,
        Trash = 2,
        Move = 3
    }

    [Header("Configuration")]
    [SerializeField] private BoosterType _boosterType;
    [SerializeField] private TMP_Text _quantityText;

    private void OnEnable()
    {
        UpdateQuantityDisplay();
    }

    public void UpdateQuantityDisplay()
    {
        if (_quantityText == null) return;
        
        if (SaveLoadManager.Data != null)
        {
            var data = SaveLoadManager.Data;
            int typeIndex = (int)_boosterType;
            
            if (typeIndex >= 0 && typeIndex < data.BoostersOwned.Count)
            {
                int count = data.BoostersOwned[typeIndex];
                if (count > 0)
                {
                    _quantityText.SetText("{0}", count);
                }
                else
                {
                    _quantityText.SetText("+"); // Hiển thị dấu + khi hết
                }
            }
        }
    }

    // Gắn hàm này vào sự kiện OnClick của Button trên Prefab
    public void OnClickButton()
    {
        if (SaveLoadManager.Data == null) return;

        var data = SaveLoadManager.Data;
        int typeIndex = (int)_boosterType;
        
        if (typeIndex < 0 || typeIndex >= data.BoostersOwned.Count) return;

        int count = data.BoostersOwned[typeIndex];

        if (count > 0)
        {
            // TODO: Logic áp dụng hiệu ứng Boost lên GridManager (cắt bánh, xoá đĩa...)
            // Khi logic gameplay của Booster hoàn thiện, bọc phần dưới đây trong if (ApplyBooster())
            // để chỉ trừ booster khi hiệu ứng thực sự áp dụng thành công.
            
            // Trừ số lượng Booster trong Data
            data.BoostersOwned[typeIndex]--;
            
            // Phát event để AchievementManager biết người chơi đã dùng Booster
            // (ảnh hưởng thành tựu LevelCompletedNoBooster)
            GameEvents.TriggerBoosterUsed();
            
            // Cập nhật UI hiển thị số lượng mới
            UpdateQuantityDisplay();
            
            Debug.Log($"[BoostButton] Đã sử dụng boost: {_boosterType}, còn lại: {data.BoostersOwned[typeIndex]}");
        }
        else
        {
            // Hết boost → Mở Shop ở Tab Boost, nhảy thẳng tới đúng loại Boost
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenShopFromGameWithItem(0, (int)_boosterType); // Tab 0 = Boost, item = loại boost
            }
        }
    }

    // Hàm tiện lợi để update tất cả BoostButton trên Scene khi ShopManager mua boost
    public static void UpdateAll()
    {
        var buttons = FindObjectsByType<BoostButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in buttons)
        {
            if (b != null) b.UpdateQuantityDisplay();
        }
    }
}
