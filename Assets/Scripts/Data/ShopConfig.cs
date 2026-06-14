using UnityEngine;

[CreateAssetMenu(fileName = "ShopConfig", menuName = "Cheezy Savoround/Shop Config")]
public class ShopConfig : ScriptableObject
{
    [Header("Danh sách Skin Đĩa (Plate)")]
    public SkinData[] Skins;

    [Header("Danh sách Booster")]
    public BoostData[] Boosters;

    [Header("Danh sách Gói Xu (Coin Packs)")]
    public CoinPackData[] CoinPacks;

    public SkinData GetSkin(string id)
    {
        if (Skins == null) return null;
        foreach (var skin in Skins)
            if (skin.Id == id) return skin;
        return null;
    }
}

[System.Serializable]
public class SkinData
{
    public string Id;
    public string DisplayName;
    public int Price;
    
    [Tooltip("Kéo thả Texture (plate01.png, plate02.png...) vào đây")]
    public Texture2D Texture;
}

[System.Serializable]
public class BoostData
{
    public string Id;
    public string DisplayName;
    public int Price; // Giá mua bằng Xu
    
    [Tooltip("Kéo thả hình ảnh 2D của Booster vào đây")]
    public Sprite Icon;
}

[System.Serializable]
public class CoinPackData
{
    public string Id;
    public string DisplayName;
    public string PriceString; // VD: "$0.99" hoặc "Ads" (Bảng Coin dùng string vì có thể mua bằng tiền thật)
    public int RewardAmount;   // Số xu nhận được khi mua gói này
    
    [Tooltip("Kéo thả hình ảnh túi tiền/rương tiền vào đây")]
    public Sprite Icon;
}
