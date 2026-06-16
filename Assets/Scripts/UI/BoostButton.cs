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

    private static readonly System.Collections.Generic.List<BoostButton> _activeButtons = new System.Collections.Generic.List<BoostButton>();

    [Header("Configuration")]
    [SerializeField] private BoosterType _boosterType;
    [SerializeField] private TMP_Text _quantityText;

    private void OnEnable()
    {
        _activeButtons.Add(this);
        UpdateQuantityDisplay();
    }

    private void OnDisable()
    {
        _activeButtons.Remove(this);
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
            if (BoosterManager.Instance != null)
            {
                BoosterManager.Instance.ActivateBooster(_boosterType, () => 
                {
                    // Trừ số lượng Booster trong Data
                    data.BoostersOwned[typeIndex]--;
                    
                    // Phát event để AchievementManager biết người chơi đã dùng Booster
                    GameEvents.TriggerBoosterUsed();
                    
                    // Cập nhật UI hiển thị số lượng mới
                    UpdateQuantityDisplay();
                    
                    Debug.Log($"[BoostButton] Đã sử dụng boost: {_boosterType}, còn lại: {data.BoostersOwned[typeIndex]}");
                });
            }
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
        for (int i = 0; i < _activeButtons.Count; i++)
        {
            if (_activeButtons[i] != null)
            {
                _activeButtons[i].UpdateQuantityDisplay();
            }
        }
    }
}
