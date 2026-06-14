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
    [SerializeField] private ShopConfig _shopConfig; // Dùng để tra giá khi mua

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
            // TODO: Logic sử dụng Boost (gọi GridManager xử lý)
            Debug.Log($"Đang sử dụng boost: {_boosterType}");
        }
        else
        {
            // Xử lý MUA boost
            if (_shopConfig != null && _shopConfig.Boosters != null && typeIndex < _shopConfig.Boosters.Length)
            {
                int price = _shopConfig.Boosters[typeIndex].Price;
                if (data.Gold >= price)
                {
                    // Trừ tiền và thêm boost
                    data.Gold -= price;
                    data.BoostersOwned[typeIndex]++;
                    SaveLoadManager.Save();
                    
                    // Cập nhật lại UI
                    UpdateQuantityDisplay();
                    
                    // Cập nhật tiền (nếu có GoldDisplay)
                    GoldDisplay.UpdateAll();
                    
                    // Phát âm thanh
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayPlaceSound();
                }
                else
                {
                    Debug.Log("Không đủ tiền mua Boost!");
                    if (AudioManager.Instance != null) AudioManager.Instance.PlayErrorSound();
                }
            }
            else
            {
                Debug.LogWarning("Chưa gắn ShopConfig hoặc cấu hình Boosters chưa đủ trong ShopConfig!");
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
